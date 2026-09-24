using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Application.Features.Claims.Queries.GetMyClaims;

public class GetMyClaimsQueryHandler : IRequestHandler<GetMyClaimsQuery, Result<IReadOnlyList<MyClaimResponseDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyClaimsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<MyClaimResponseDto>>> Handle(GetMyClaimsQuery request, CancellationToken cancellationToken)
    {
        var claims = await _unitOfWork.Claims.GetQueryable(asNoTracking: true)
            .Where(c => c.ClaimantId == request.ClaimantId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new MyClaimResponseDto(
                c.Id,
                c.ItemId,
                c.Item.Title,
                c.Item.Type.ToString(),
                c.Status.ToString(),
                c.Message,
                c.CreatedAt,
                c.ReviewedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<MyClaimResponseDto>>.Success(claims);
    }
}
