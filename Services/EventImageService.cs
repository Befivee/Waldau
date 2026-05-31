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

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateUpload(file.Length, file.FileName, file.ContentType);

        var extension = Path.GetExtension(file.FileName);
        var uploadsDir = GetUploadsDirectory("events");
        var fileName = $"{Guid.NewGuid():N}{extension!.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream, cancellationToken);

        return BuildPublicPath("events", fileName);
    }

    public async Task<string> SaveFromStreamAsync(
        Stream stream,
        string extension,
        string uploadSubfolder = "events",
        CancellationToken cancellationToken = default)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Допустимые форматы: JPG, JPEG, PNG, WEBP.");

        if (stream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("Максимальный размер файла — 10 МБ.");

        var folder = NormalizeUploadSubfolder(uploadSubfolder);
        var uploadsDir = GetUploadsDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        await using var fileStream = File.Create(physicalPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return BuildPublicPath(folder, fileName);
    }

    public Task DeleteIfUploadedAsync(string? imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) ||
            !imagePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(environment.WebRootPath, relativePath);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        return Task.CompletedTask;
    }

    private string GetUploadsDirectory(string uploadSubfolder)
    {
        var uploadsDir = Path.Combine(environment.WebRootPath, "uploads", uploadSubfolder);
        Directory.CreateDirectory(uploadsDir);
        return uploadsDir;
    }

    private static string NormalizeUploadSubfolder(string uploadSubfolder)
    {
        var folder = uploadSubfolder.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(folder) ||
            folder.Contains("..", StringComparison.Ordinal) ||
            folder.Contains(Path.DirectorySeparatorChar) ||
            folder.Contains('/'))
        {
            throw new InvalidOperationException("Недопустимая папка для загрузки изображения.");
        }

        return folder;
    }

    private static string BuildPublicPath(string uploadSubfolder, string fileName) =>
        $"/uploads/{uploadSubfolder}/{fileName}";

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
