namespace WaldauCastle.Services;

public class EventImageService(IWebHostEnvironment environment) : IEventImageService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly Dictionary<string, HashSet<string>> AllowedMimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
            [".jpeg"] = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
            [".png"] = new(StringComparer.OrdinalIgnoreCase) { "image/png" },
            [".webp"] = new(StringComparer.OrdinalIgnoreCase) { "image/webp" }
        };

    private const string UploadFolder = "uploads/events";
    private const string PublicPrefix = "/uploads/events/";

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateUpload(file.Length, file.FileName, file.ContentType);

        var extension = Path.GetExtension(file.FileName);
        var uploadsDir = GetUploadsDirectory();
        var fileName = $"{Guid.NewGuid():N}{extension!.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream, cancellationToken);

        return PublicPrefix + fileName;
    }

    public async Task<string> SaveFromStreamAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Допустимые форматы: JPG, JPEG, PNG, WEBP.");

        if (stream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("Максимальный размер файла — 10 МБ.");

        var uploadsDir = GetUploadsDirectory();
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using var fileStream = File.Create(physicalPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return PublicPrefix + fileName;
    }

    public Task DeleteIfUploadedAsync(string? imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var fileName = Path.GetFileName(imagePath);
        var physicalPath = Path.Combine(GetUploadsDirectory(), fileName);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        return Task.CompletedTask;
    }

    private string GetUploadsDirectory()
    {
        var uploadsDir = Path.Combine(environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(uploadsDir);
        return uploadsDir;
    }

    private static void ValidateUpload(long length, string fileName, string? contentType)
    {
        if (length == 0)
            throw new InvalidOperationException("Файл изображения пуст.");

        if (length > MaxFileSizeBytes)
            throw new InvalidOperationException("Максимальный размер файла — 10 МБ.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Допустимые форматы: JPG, JPEG, PNG, WEBP.");

        if (!string.IsNullOrWhiteSpace(contentType) &&
            AllowedMimeTypes.TryGetValue(extension, out var allowedMimes) &&
            !allowedMimes.Contains(contentType))
        {
            throw new InvalidOperationException("Недопустимый MIME-тип файла.");
        }
    }
}
