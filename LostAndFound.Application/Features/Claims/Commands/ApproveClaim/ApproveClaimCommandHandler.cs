using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Claims.Commands.ApproveClaim;

public class ApproveClaimCommandHandler : IRequestHandler<ApproveClaimCommand, Result<ClaimResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveClaimCommandHandler> _logger;

    public ApproveClaimCommandHandler(IUnitOfWork unitOfWork, ILogger<ApproveClaimCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ClaimResponseDto>> Handle(ApproveClaimCommand request, CancellationToken cancellationToken)
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

        // Only item owner or Admin can approve
        if (!request.IsAdmin && item.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to approve claim {ClaimId} on item {ItemId} by user {UserId}", request.ClaimId, item.Id, request.CurrentUserId);
            return Result<ClaimResponseDto>.Failure("You are not authorized to approve claims for this item.", ResultErrorType.Forbidden);
        }

        // Claim must be Pending
        if (claim.Status != ClaimStatus.Pending)
        {
            _logger.LogWarning("Approval rejected for claim {ClaimId}: Status is {Status}, not Pending", claim.Id, claim.Status);
            return Result<ClaimResponseDto>.Failure($"Only pending claims can be approved. Current claim status is {claim.Status}.", ResultErrorType.Conflict);
        }

        // Associated item must be Active
        if (item.Status != ItemStatus.Active)
        {
            _logger.LogWarning("Approval rejected for claim {ClaimId}: Associated item {ItemId} status is {Status}, not Active", claim.Id, item.Id, item.Status);
            return Result<ClaimResponseDto>.Failure($"Only active items can have their claims approved. Current item status is {item.Status}.", ResultErrorType.Conflict);
        }

        var now = DateTime.UtcNow;

        // 1. Approve selected claim
        claim.Status = ClaimStatus.Approved;
        claim.ReviewedAt = now;
        _unitOfWork.Claims.Update(claim);

        // 2. Set item status = Claimed
        item.Status = ItemStatus.Claimed;
        item.UpdatedAt = now;
        _unitOfWork.Items.Update(item);

        // 3. Atomically reject all other pending claims for this item
        var otherPendingClaims = await _unitOfWork.Claims.GetPendingClaimsForItemAsync(claim.ItemId, cancellationToken);
        foreach (var other in otherPendingClaims)
        {
            if (other.Id != claim.Id)
            {
                other.Status = ClaimStatus.Rejected;
                other.ReviewedAt = now;
            }
        }

        if (otherPendingClaims.Count > 0)
        {
            _unitOfWork.Claims.UpdateRange(otherPendingClaims);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Claim {ClaimId} approved for item {ItemId} by user {UserId}. Item status transitioned to Claimed.", claim.Id, item.Id, request.CurrentUserId);

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
