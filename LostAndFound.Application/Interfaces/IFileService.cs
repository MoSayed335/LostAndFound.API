using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Interfaces;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string? relativeUrl, CancellationToken cancellationToken = default);

    bool ValidateImage(IFormFile? file, out string? errorMessage);
}
