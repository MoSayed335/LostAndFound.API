using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Matching.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Matching.Queries.GetItemMatches;

public class GetItemMatchesQueryHandler : IRequestHandler<GetItemMatchesQuery, Result<IReadOnlyList<MatchResultDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchingService _matchingService;
    private readonly ILogger<GetItemMatchesQueryHandler> _logger;

    public GetItemMatchesQueryHandler(
        IUnitOfWork unitOfWork,
        IMatchingService matchingService,
        ILogger<GetItemMatchesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _matchingService = matchingService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MatchResultDto>>> Handle(GetItemMatchesQuery request, CancellationToken cancellationToken)
    {
        var sourceItem = await _unitOfWork.Items.GetByIdWithDetailsAsync(request.ItemId, asNoTracking: true, cancellationToken);
        if (sourceItem is null)
        {
            return Result<IReadOnlyList<MatchResultDto>>.Failure("Item not found.", ResultErrorType.NotFound);
        }

        // Authorization: caller must be item owner or an Admin
        if (!request.IsAdmin && sourceItem.UserId != request.CurrentUserId)
        {
            _logger.LogWarning("Unauthorized attempt to view matches for item {ItemId} by user {UserId}", request.ItemId, request.CurrentUserId);
            return Result<IReadOnlyList<MatchResultDto>>.Failure(
                "You are not authorized to view matches for this item.",
                ResultErrorType.Forbidden);
        }

        // If source item is not Active (e.g. Claimed, Returned, Closed), it cannot have active matches
        if (sourceItem.Status != ItemStatus.Active)
        {
            return Result<IReadOnlyList<MatchResultDto>>.Success(Array.Empty<MatchResultDto>());
        }

        // Only match opposite types: Lost <-> Found
        var oppositeType = sourceItem.Type == ItemType.Lost ? ItemType.Found : ItemType.Lost;

        var candidates = await _unitOfWork.Items.GetActiveItemsByTypeAsync(oppositeType, sourceItem.Id, cancellationToken);

        _logger.LogInformation(
            "Matching item {ItemId} ({Type}) against {CandidateCount} active candidate {OppositeType} items",
            sourceItem.Id,
            sourceItem.Type,
            candidates.Count,
            oppositeType);

        var matches = _matchingService.FindMatches(sourceItem, candidates);

        return Result<IReadOnlyList<MatchResultDto>>.Success(matches);
    }
}
