namespace LostAndFound.Application.Features.Items.DTOs;

public record ItemResponseDto(
    int Id,
    string Title,
    string Description,
    string Type,
    int CategoryId,
    string CategoryName,
    string Location,
    DateTime DateLostOrFound,
    string? ImageUrl,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    UserSummaryDto? Owner
);
