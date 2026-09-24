namespace LostAndFound.Application.Features.Claims.DTOs;

public record ClaimResponseDto(
    int Id,
    int ItemId,
    int ClaimantId,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
