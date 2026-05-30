using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using WaldauCastle.Data;
using WaldauCastle.Options;
using WaldauCastle.Services;
using WaldauCastle.Services.Telegram;
using WaldauCastle.Services.VK;

var builder = WebApplication.CreateBuilder(args);

// Production behind nginx: bind HTTP on all interfaces (required for reverse proxy).
// Without this, Linux publish may try HTTPS :5001 without a cert and Kestrel never starts.
var urls = builder.Configuration["ASPNETCORE_URLS"];
if (string.IsNullOrWhiteSpace(urls))
    builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));
builder.Services.Configure<VKOptions>(
    builder.Configuration.GetSection(VKOptions.SectionName));
builder.Services.Configure<SiteSettings>(
    builder.Configuration.GetSection(SiteSettings.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

var sqliteConnectionString = ResolveSqliteConnectionString(builder.Configuration, builder.Environment);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));

builder.Services.AddScoped<IExcursionService, ExcursionService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEventImageService, EventImageService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddSingleton<TelegramStateService>();
builder.Services.AddScoped<TelegramEventManager>();

var telegramOptions = builder.Configuration
    .GetSection(TelegramBotOptions.SectionName)
    .Get<TelegramBotOptions>() ?? new TelegramBotOptions();

if (telegramOptions.IsConfigured)
{
    var botToken = telegramOptions.BotToken.Trim();

    builder.Services.AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((_, _) => new TelegramBotClient(botToken));

    builder.Services.AddSingleton<TelegramCommandHandler>();
    builder.Services.AddHostedService<TelegramBotService>();
    builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
}
else
{
    builder.Services.AddScoped<ITelegramNotificationService, NullTelegramNotificationService>();
}

var vkOptions = builder.Configuration
    .GetSection(VKOptions.SectionName)
    .Get<VKOptions>() ?? new VKOptions();

if (vkOptions.IsConfigured)
{
    builder.Services.AddHttpClient<VKApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });
    builder.Services.AddSingleton<VKMessageHandler>();
    builder.Services.AddHostedService<VKBotService>();
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!telegramOptions.IsConfigured)
{
    app.Logger.LogWarning("Telegram-бот отключён: укажите корректные BotToken и AdminChatId.");
}

if (!vkOptions.IsConfigured)
{
    app.Logger.LogWarning("VK-бот отключён: укажите VK:AccessToken и VK:GroupId.");
}

Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "events"));
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "Backups"));

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
{
    await DbInitializer.SeedAsync(db);
    app.Logger.LogInformation("DB seeded successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "DB seed failed — app will continue startup");
}
    app.Logger.LogInformation("База данных SQLite инициализирована: {DbPath}", sqliteConnectionString);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Не удалось инициализировать базу данных. Проверьте путь к waldau.db и права на запись.");
    throw;
}

if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Home/ServerError");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Необработанное исключение: {Method} {Path}", context.Request.Method, context.Request.Path);
        throw;
    }
});

app.UseStatusCodePagesWithReExecute("/Home/StatusCodeError/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapStaticAssets();

app.MapControllerRoute(
    name: "legacy-tours",
    pattern: "Tours/{action=Index}/{id?}",
    defaults: new { controller = "Excursion", action = "Index" });

app.MapControllerRoute(
    name: "legacy-tours-book",
    pattern: "Tours/Book",
    defaults: new { controller = "Booking", action = "Create" });

app.MapControllerRoute(
    name: "legacy-events",
    pattern: "Events/{action=Index}/{id?}",
    defaults: new { controller = "Event", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Logger.LogInformation(
    "Запуск веб-сервера Kestrel (окружение: {Environment}, URLs: {Urls})",
    app.Environment.EnvironmentName,
    urls ?? builder.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://0.0.0.0:5000");

app.Run();

static string ResolveSqliteConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=/var/www/waldau/waldau.db";

    const string prefix = "Data Source=";
    if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return connectionString;

    var dataSource = connectionString[prefix.Length..].Trim().Trim('"');
    if (Path.IsPathRooted(dataSource))
        return $"Data Source={dataSource}";

    var absolutePath = Path.Combine(environment.ContentRootPath, dataSource);
    return $"Data Source={absolutePath}";
}
