namespace LostAndFound.Application.Features.Items.DTOs;

public record CreateItemRequest(
    string Title,
    string Description,
    int CategoryId,
    string Location,
    DateTime DateLostOrFound,
    string? ImageUrl = null
);
