using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Items.Queries.GetMyItems;

public class GetMyItemsQueryHandler : IRequestHandler<GetMyItemsQuery, Result<PaginatedList<ItemResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyItemsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaginatedList<ItemResponseDto>>> Handle(GetMyItemsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Items.GetQueryable(asNoTracking: true)
            .Where(i => i.UserId == request.UserId);

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
