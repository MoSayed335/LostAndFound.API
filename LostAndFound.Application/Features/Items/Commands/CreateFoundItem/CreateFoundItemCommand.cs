using LostAndFound.Application.Features.Items.Commands.Common;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Features.Items.Commands.CreateFoundItem;

public record CreateFoundItemCommand(
    string Title,
    string Description,
    int CategoryId,
    string Location,
    DateTime DateLostOrFound,
    string? ImageUrl,
    int UserId,
    IFormFile? Image = null
) : CreateItemCommandBase(Title, Description, CategoryId, Location, DateLostOrFound, ImageUrl, UserId, Image);

