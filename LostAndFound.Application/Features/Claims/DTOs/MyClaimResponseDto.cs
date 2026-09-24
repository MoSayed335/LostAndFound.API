namespace LostAndFound.Application.Features.Claims.DTOs;

public record MyClaimResponseDto(
    int Id,
    int ItemId,
    string ItemTitle,
    string ItemType,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
