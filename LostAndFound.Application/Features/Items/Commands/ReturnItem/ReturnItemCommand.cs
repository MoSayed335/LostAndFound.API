using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Items.Commands.ReturnItem;

public record ReturnItemCommand(
    int ItemId,
    int CurrentUserId,
    bool IsAdmin
) : IRequest<Result<ItemResponseDto>>;
