using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Domain.Enums;
using MediatR;

namespace LostAndFound.Application.Features.Items.Queries.GetItems;

public record GetItemsQuery(
    ItemType? Type = null,
    int? CategoryId = null,
    ItemStatus? Status = null,
    string? Location = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<ItemResponseDto>>>;
