namespace LostAndFound.Application.Features.Claims.DTOs;

public record ItemClaimResponseDto(
    int Id,
    int ClaimantId,
    string ClaimantName,
    string ClaimantEmail,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
