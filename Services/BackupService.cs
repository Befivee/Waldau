using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WaldauCastle.Data;

namespace WaldauCastle.Services;

public class BackupService(ApplicationDbContext db, IWebHostEnvironment environment, ILogger<BackupService> logger) : IBackupService
{
    private const string BackupFolder = "App_Data/Backups";

    public async Task<string> ExportBookingsAndEventsAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = Path.Combine(environment.ContentRootPath, BackupFolder);
        Directory.CreateDirectory(backupDir);

        var payload = new
        {
            ExportedAt = DateTime.UtcNow,
            Events = await db.Events.AsNoTracking().ToListAsync(cancellationToken),
            Bookings = await db.Bookings.AsNoTracking().ToListAsync(cancellationToken)
        };

        var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(backupDir, fileName);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }, cancellationToken);

        logger.LogInformation("Резервная копия создана: {BackupPath}", filePath);
        return filePath;
    }
}
