namespace WaldauCastle.Services;

public interface IEventImageService
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task<string> SaveFromStreamAsync(
        Stream stream,
        string extension,
        string uploadSubfolder = "events",
        CancellationToken cancellationToken = default);

    Task DeleteIfUploadedAsync(string? imagePath, CancellationToken cancellationToken = default);
}
