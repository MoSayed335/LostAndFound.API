using LostAndFound.Domain.Enums;

namespace LostAndFound.Application.Features.Items.DTOs;

public record UpdateItemRequest(
    string Title,
    string Description,
    int CategoryId,
    string Location,
    DateTime DateLostOrFound,
    string? ImageUrl = null,
    ItemStatus? Status = null
);
