using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using WaldauCastle.Data;
using WaldauCastle.Options;
using WaldauCastle.Services;
using WaldauCastle.Services.Bot;
using WaldauCastle.Services.Seo;
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
builder.Services.Configure<AnalyticsOptions>(
    builder.Configuration.GetSection(AnalyticsOptions.SectionName));

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
builder.Services.AddHostedService<BookingCleanupService>();
builder.Services.AddScoped<IEventImageService, EventImageService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<CastleAdminContentService>();
builder.Services.AddSingleton<TelegramStateService>();
builder.Services.AddScoped<TelegramEventManager>();

var telegramOptions = builder.Configuration
    .GetSection(TelegramBotOptions.SectionName)
    .Get<TelegramBotOptions>() ?? new TelegramBotOptions();

var botOnly = telegramOptions.BotOnly;
var useTelegramRelay = !string.IsNullOrWhiteSpace(telegramOptions.RelayUrl);

if (botOnly)
    builder.WebHost.UseUrls("http://127.0.0.1:5000");

if (useTelegramRelay)
{
    builder.Services.AddHttpClient("telegram_relay", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(20);
    });
    builder.Services.AddSingleton<ITelegramNotificationService, RelayTelegramNotificationService>();
}
else if (telegramOptions.IsConfigured)
{
    var botToken = telegramOptions.BotToken.Trim();
    var clientOptions = new TelegramBotClientOptions(botToken) { RetryCount = 2 };

    builder.Services.AddHttpClient("telegram_polling", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateTelegramHttpHandler(telegramOptions))
        .AddTypedClient<ITelegramBotClient>((httpClient, _) =>
            new TelegramBotClient(clientOptions, httpClient));

    builder.Services.AddHttpClient("telegram_notifications", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateTelegramHttpHandler(telegramOptions));

    builder.Services.AddSingleton<ITelegramNotificationService>(sp =>
    {
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("telegram_notifications");
        return new TelegramNotificationService(
            new TelegramBotClient(clientOptions, httpClient),
            sp.GetRequiredService<IOptions<TelegramBotOptions>>(),
            sp.GetRequiredService<ILogger<TelegramNotificationService>>());
    });

    builder.Services.AddSingleton<TelegramCommandHandler>();
    if (!telegramOptions.DisablePolling)
        builder.Services.AddHostedService<TelegramBotService>();
}
else
{
    builder.Services.AddSingleton<ITelegramNotificationService, NullTelegramNotificationService>();
}

builder.Services.AddSingleton<BookingNotificationQueue>();
builder.Services.AddHostedService<BookingNotificationWorker>();
builder.Services.AddScoped<IBookingNotificationService, BookingNotificationService>();

builder.Services.AddHttpClient("indexnow", client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHostedService<IndexNowPingerService>();

var vkOptions = VKOptions.Load(builder.Configuration);
var vkValidation = vkOptions.Validate();

if (vkValidation.IsValid)
{
    builder.Services.AddHttpClient<VKApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });
    builder.Services.AddHttpClient("vk_photo_download");
    builder.Services.AddSingleton<VKStateService>();
    builder.Services.AddScoped<VKAdminManager>();
    builder.Services.AddSingleton<VKCommandHandler>();
    builder.Services.AddHostedService<VKBotService>();
    builder.Services.AddScoped<IVKNotificationService, VKNotificationService>();
}
else
{
    builder.Services.AddScoped<IVKNotificationService, NullVKNotificationService>();
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (useTelegramRelay)
{
    app.Logger.LogInformation("Telegram-уведомления уходят на relay {RelayUrl}", telegramOptions.RelayUrl);
}
else if (!telegramOptions.IsConfigured)
{
    app.Logger.LogWarning("Telegram-бот отключён: укажите корректные BotToken и AdminChatId.");
}
else if (telegramOptions.DisablePolling)
{
    app.Logger.LogInformation("Telegram long polling выключен (бот на другом сервере).");
}

if (!vkValidation.IsValid)
{
    app.Logger.LogWarning(
        "VK-бот отключён (VKBotService не зарегистрирован). Причины: {ValidationErrors}",
        vkValidation.Summary);

    foreach (var error in vkValidation.Errors)
        app.Logger.LogWarning("VK config: {Error}", error);

    LogVkConfigurationSources(app.Logger, builder.Configuration);
}
else
{
    vkOptions.TryGetGroupId(out var vkGroupId);
    app.Logger.LogInformation(
        "VK-бот зарегистрирован: VKBotService + VKApiClient (group {GroupId}, api {ApiVersion}).",
        vkGroupId,
        vkOptions.ApiVersion);
}

if (!botOnly)
{
    Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "events"));
    Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "excursions"));
}
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "Backups"));

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
{
    await DbInitializer.InitializeAsync(db);
    app.Logger.LogInformation("Миграции базы данных применены");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Не удалось применить миграции — приложение продолжит запуск");
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
    if (!botOnly)
    {
        app.UseExceptionHandler("/Home/ServerError");
        app.UseHsts();
    }
}
else
{
    app.UseDeveloperExceptionPage();
    if (!botOnly)
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

if (!botOnly)
    app.UseStatusCodePagesWithReExecute("/Home/StatusCodeError/{0}");

if (!botOnly)
    app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (telegramOptions.AcceptRelay)
{
    app.MapPost("/internal/telegram/booking", async (
        HttpRequest request,
        ITelegramNotificationService telegram,
        IOptions<TelegramBotOptions> botOptions) =>
    {
        var expected = botOptions.Value.RelaySecret?.Trim() ?? "";
        var provided = request.Headers["X-Relay-Secret"].ToString();
        if (expected.Length == 0 || !CryptographicEquals(expected, provided))
            return Results.Unauthorized();

        var payload = await request.ReadFromJsonAsync<BookingRelayPayload>();
        if (payload is null)
            return Results.BadRequest();

        var sent = await telegram.NotifyNewBookingAsync(payload.ToBooking());
        return sent ? Results.Ok() : Results.StatusCode(502);
    });
}

if (!botOnly)
{
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
}

app.Logger.LogInformation(
    "Запуск веб-сервера Kestrel (окружение: {Environment}, URLs: {Urls})",
    app.Environment.EnvironmentName,
    urls ?? builder.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://0.0.0.0:5000");

app.Run();

static void LogVkConfigurationSources(ILogger logger, IConfiguration configuration)
{
    var accessTokenSource = configuration.GetSection("VK:AccessToken").Value is { Length: > 0 } ? "set" : "missing";
    var groupIdRaw = configuration["VK:GroupId"];
    var groupIdSource = string.IsNullOrWhiteSpace(groupIdRaw) ? "missing" : $"raw='{groupIdRaw}'";
    var apiVersion = configuration["VK:ApiVersion"] ?? "(default)";
    var waitSeconds = configuration["VK:LongPollWaitSeconds"] ?? "(default)";

    logger.LogWarning(
        "VK config snapshot — AccessToken: {AccessTokenState}, GroupId: {GroupIdState}, ApiVersion: {ApiVersion}, LongPollWaitSeconds: {WaitSeconds}. " +
        "Env vars: VK__AccessToken, VK__GroupId, VK__ApiVersion, VK__LongPollWaitSeconds.",
        accessTokenSource,
        groupIdSource,
        apiVersion,
        waitSeconds);
}

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

static bool CryptographicEquals(string expected, string provided)
{
    var a = System.Text.Encoding.UTF8.GetBytes(expected);
    var b = System.Text.Encoding.UTF8.GetBytes(provided);
    if (a.Length != b.Length)
        return false;
    return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
}

static HttpMessageHandler CreateTelegramHttpHandler(TelegramBotOptions telegram)
{
    var handler = new HttpClientHandler();
    if (telegram.TryGetProxyUri(out var proxyUri) && proxyUri is not null)
    {
        handler.Proxy = new WebProxy(proxyUri);
        handler.UseProxy = true;
    }

    return handler;
}
