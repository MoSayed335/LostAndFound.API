using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Items.Queries.GetMyItems;

public record GetMyItemsQuery(
    int UserId,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<ItemResponseDto>>>;
