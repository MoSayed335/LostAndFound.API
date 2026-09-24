using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.Common.Options;
using LostAndFound.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LostAndFound.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ImageUploadOptions _options;
    private readonly ILogger<FileService> _logger;

    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffHeader = [0x52, 0x49, 0x46, 0x46]; // "RIFF"
    private static readonly byte[] WebpHeader = [0x57, 0x45, 0x42, 0x50]; // "WEBP"

    public FileService(
        IWebHostEnvironment environment,
        IOptions<ImageUploadOptions> options,
        ILogger<FileService> logger)
    {
        _environment = environment;
        _options = options?.Value ?? new ImageUploadOptions();
        _logger = logger;
    }

    public bool ValidateImage(IFormFile? file, out string? errorMessage)
    {
        if (file is null)
        {
            errorMessage = null;
            return true;
        }

        if (file.Length == 0)
        {
            errorMessage = "The uploaded file is empty.";
            return false;
        }

        if (file.Length > _options.MaxFileSizeInBytes)
        {
            long maxMb = _options.MaxFileSizeInBytes / (1024 * 1024);
            errorMessage = $"File size ({file.Length / (1024 * 1024)} MB) exceeds the maximum allowed limit of {maxMb} MB.";
            return false;
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errorMessage = $"File extension '{extension}' is not allowed. Supported formats: {string.Join(", ", _options.AllowedExtensions)}.";
            return false;
        }

        // Validate actual file binary magic number
        if (!IsValidImageHeader(file, extension))
        {
            errorMessage = "File content does not match a valid image format.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken cancellationToken = default)
    {
        if (!ValidateImage(file, out string? error))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Image", error ?? "Invalid image file.")]);
        }

        string webRoot = GetWebRootPath();
        string targetFolder = string.IsNullOrWhiteSpace(subFolder)
            ? _options.UploadFolder
            : Path.Combine("uploads", subFolder).Replace('\\', '/');

        string fullDirPath = Path.Combine(webRoot, targetFolder);
        string canonicalDir = Path.GetFullPath(fullDirPath);
        string canonicalWebRoot = Path.GetFullPath(webRoot);

        // Path traversal security check
        if (!canonicalDir.StartsWith(canonicalWebRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Potential path traversal attempt detected with targetFolder: {TargetFolder}", targetFolder);
            throw new InvalidOperationException("Invalid destination folder path.");
        }

        Directory.CreateDirectory(canonicalDir);

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string fullFilePath = Path.Combine(canonicalDir, uniqueFileName);

        await using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        string relativeUrl = $"/{targetFolder}/{uniqueFileName}".Replace('\\', '/').Replace("//", "/");
        _logger.LogInformation("Image successfully stored at relative path: {RelativeUrl}", relativeUrl);

        return relativeUrl;
    }

    public Task DeleteFileAsync(string? relativeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return Task.CompletedTask;
        }

        try
        {
            string webRoot = GetWebRootPath();
            string relativePath = relativeUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string fullPhysicalPath = Path.Combine(webRoot, relativePath);

            string canonicalPath = Path.GetFullPath(fullPhysicalPath);
            string canonicalWebRoot = Path.GetFullPath(webRoot);

            if (!canonicalPath.StartsWith(canonicalWebRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Security: Attempted deletion outside web root: {RelativeUrl}", relativeUrl);
                return Task.CompletedTask;
            }

            if (File.Exists(canonicalPath))
            {
                File.Delete(canonicalPath);
                _logger.LogInformation("Deleted image file: {CanonicalPath}", canonicalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at relative path: {RelativeUrl}", relativeUrl);
        }

        return Task.CompletedTask;
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            return _environment.WebRootPath;
        }

        string fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (!Directory.Exists(fallback))
        {
            Directory.CreateDirectory(fallback);
        }

        return fallback;
    }

    private static bool IsValidImageHeader(IFormFile file, string extension)
    {
        try
        {
            using var stream = file.OpenReadStream();
            byte[] header = new byte[12];
            int bytesRead = stream.Read(header, 0, header.Length);

            if (bytesRead < 3)
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == JpegHeader[0] && header[1] == JpegHeader[1] && header[2] == JpegHeader[2],
                ".png" => bytesRead >= 8 && header.Take(8).SequenceEqual(PngHeader),
                ".webp" => bytesRead >= 12
                           && header.Take(4).SequenceEqual(RiffHeader)
                           && header.Skip(8).Take(4).SequenceEqual(WebpHeader),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
