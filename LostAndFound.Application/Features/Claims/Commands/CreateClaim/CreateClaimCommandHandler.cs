using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Claims.Commands.CreateClaim;

public class CreateClaimCommandHandler : IRequestHandler<CreateClaimCommand, Result<ClaimResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateClaimCommandHandler> _logger;
    private readonly IBackgroundJobScheduler? _backgroundJobScheduler;

    public CreateClaimCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateClaimCommandHandler> logger,
        IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    public async Task<Result<ClaimResponseDto>> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Items.GetByIdAsync(request.ItemId, asNoTracking: true, cancellationToken);
        if (item is null)
        {
            return Result<ClaimResponseDto>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // User cannot claim their own item
        if (item.UserId == request.ClaimantId)
        {
            _logger.LogWarning("User {UserId} attempted to claim their own item {ItemId}", request.ClaimantId, request.ItemId);
            return Result<ClaimResponseDto>.Failure("You cannot claim your own item.", ResultErrorType.Conflict);
        }

        // Only Active items can be claimed
        if (item.Status != ItemStatus.Active)
        {
            _logger.LogWarning("Claim rejected for item {ItemId}: Item is not Active (Status: {Status})", request.ItemId, item.Status);
            return Result<ClaimResponseDto>.Failure("Only active items can be claimed.", ResultErrorType.Conflict);
        }

        // User cannot have another pending claim for the same item
        var hasPending = await _unitOfWork.Claims.HasPendingClaimAsync(request.ItemId, request.ClaimantId, cancellationToken);
        if (hasPending)
        {
            _logger.LogWarning("User {UserId} already has a pending claim for item {ItemId}", request.ClaimantId, request.ItemId);
            return Result<ClaimResponseDto>.Failure("You already have a pending claim for this item.", ResultErrorType.Conflict);
        }

        var claim = new Claim
        {
            ItemId = request.ItemId,
            ClaimantId = request.ClaimantId,
            Message = request.Message,
            Status = ClaimStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Claims.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Claim {ClaimId} created on item {ItemId} by claimant {ClaimantId}", claim.Id, claim.ItemId, claim.ClaimantId);

        // 2. Delayed Job: Schedule a reminder check in 24 hours if claim remains unreviewed
        _backgroundJobScheduler?.Schedule<IBackgroundJobService>(
            service => service.CheckPendingClaimReminderAsync(claim.Id, CancellationToken.None),
            TimeSpan.FromHours(24));

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
