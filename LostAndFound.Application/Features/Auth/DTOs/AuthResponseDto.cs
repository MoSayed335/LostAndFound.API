namespace LostAndFound.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    int UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime ExpiresAtUtc);
