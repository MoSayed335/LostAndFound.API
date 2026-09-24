using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Claims.Commands.RejectClaim;

public class RejectClaimCommandHandler : IRequestHandler<RejectClaimCommand, Result<ClaimResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectClaimCommandHandler> _logger;

    public RejectClaimCommandHandler(IUnitOfWork unitOfWork, ILogger<RejectClaimCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ClaimResponseDto>> Handle(RejectClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _unitOfWork.Claims.GetByIdWithItemAndClaimantAsync(request.ClaimId, cancellationToken);
        if (claim is null)
        {
            return Result<ClaimResponseDto>.Failure("Claim not found.", ResultErrorType.NotFound);
        }

        var item = claim.Item;
        if (item is null)
        {
            return Result<ClaimResponseDto>.Failure("Associated item not found.", ResultErrorType.NotFound);
        }

        // Only item owner or Admin can reject
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to reject claim {ClaimId} on item {ItemId} by user {UserId}", request.ClaimId, item.Id, request.CurrentUserId);
            return Result<ClaimResponseDto>.Failure("You are not authorized to reject claims for this item.", ResultErrorType.Forbidden);
        }

        // Claim must be Pending
        if (claim.Status != ClaimStatus.Pending)
        {
            _logger.LogWarning("Rejection failed for claim {ClaimId}: Status is {Status}, not Pending", claim.Id, claim.Status);
            return Result<ClaimResponseDto>.Failure($"Only pending claims can be rejected. Current claim status is {claim.Status}.", ResultErrorType.Conflict);
        }

        claim.Status = ClaimStatus.Rejected;
        claim.ReviewedAt = DateTime.UtcNow;

        _unitOfWork.Claims.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Claim {ClaimId} on item {ItemId} rejected by user {UserId}", claim.Id, item.Id, request.CurrentUserId);

        var response = new ClaimResponseDto(
            claim.Id,
            claim.ItemId,
            claim.ClaimantId,
            claim.Message,
            claim.Status.ToString(),
            claim.CreatedAt,
            claim.ReviewedAt
        );

        return Result<ClaimResponseDto>.Success(response);
    }
}
