using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Items.Queries.GetItems;

public class GetItemsQueryHandler : IRequestHandler<GetItemsQuery, Result<PaginatedList<ItemResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetItemsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaginatedList<ItemResponseDto>>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Items.GetQueryable(asNoTracking: true);

        if (request.Type.HasValue)
        {
            query = query.Where(i => i.Type == request.Type.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == request.CategoryId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(i => i.Location.Contains(request.Location));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.DateLostOrFound >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.DateLostOrFound <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(i => i.Title.Contains(search) || i.Description.Contains(search) || i.Location.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new ItemResponseDto(
                i.Id,
                i.Title,
                i.Description,
                i.Type.ToString(),
                i.CategoryId,
                i.Category.Name,
                i.Location,
                i.DateLostOrFound,
                i.ImageUrl,
                i.Status.ToString(),
                i.CreatedAt,
                i.UpdatedAt,
                new UserSummaryDto(i.User.Id, i.User.FirstName, i.User.LastName, i.User.Email ?? string.Empty)
            ))
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<ItemResponseDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Result<PaginatedList<ItemResponseDto>>.Success(result);
    }
}
