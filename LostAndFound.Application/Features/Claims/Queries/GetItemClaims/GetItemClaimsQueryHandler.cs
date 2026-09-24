using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Claims.Queries.GetItemClaims;

public class GetItemClaimsQueryHandler : IRequestHandler<GetItemClaimsQuery, Result<IReadOnlyList<ItemClaimResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetItemClaimsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<ItemClaimResponseDto>>> Handle(GetItemClaimsQuery request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(request.ItemId, asNoTracking: true, cancellationToken);
        if (item is null)
        {
            return Result<IReadOnlyList<ItemClaimResponseDto>>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // Only owner or Admin can view item claims
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            return Result<IReadOnlyList<ItemClaimResponseDto>>.Failure("You are not authorized to view claims for this item.", ResultErrorType.Forbidden);
        }

        var claims = await _unitOfWork.Claims.GetQueryable(asNoTracking: true)
            .Where(c => c.ItemId == request.ItemId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ItemClaimResponseDto(
                c.Id,
                c.ClaimantId,
                $"{c.Claimant.FirstName} {c.Claimant.LastName}".Trim(),
                c.Claimant.Email ?? string.Empty,
                c.Message,
                c.Status.ToString(),
                c.CreatedAt,
                c.ReviewedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ItemClaimResponseDto>>.Success(claims);
    }
}
