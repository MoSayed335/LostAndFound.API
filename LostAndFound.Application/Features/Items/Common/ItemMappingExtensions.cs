using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Features.Items.Common;

public static class ItemMappingExtensions
{
    public static ItemResponseDto ToResponseDto(this Item item)
    {
        return new ItemResponseDto(
            item.Id,
            item.Title,
            item.Description,
            item.Type.ToString(),
            item.CategoryId,
            item.Category?.Name ?? string.Empty,
            item.Location,
            item.DateLostOrFound,
            item.ImageUrl,
            item.Status.ToString(),
            item.CreatedAt,
            item.UpdatedAt,
            item.User is not null
                ? new UserSummaryDto(
                    item.User.Id,
                    item.User.FirstName,
                    item.User.LastName,
                    item.User.Email ?? string.Empty)
                : null
        );
    }
}
