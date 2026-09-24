using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Features.Items.Commands.UpdateItem;

public record UpdateItemCommand(
    int Id,
    string Title,
    string Description,
    int CategoryId,
    string Location,
    DateTime DateLostOrFound,
    string? ImageUrl,
    ItemStatus? Status,
    int CurrentUserId,
    bool IsAdmin,
    IFormFile? Image = null
) : IRequest<Result<ItemResponseDto>>;

