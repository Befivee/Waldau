namespace WaldauCastle.Services;

public interface IBackupService
{
    Task<string> ExportBookingsAndEventsAsync(CancellationToken cancellationToken = default);
}
