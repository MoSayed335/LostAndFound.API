using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Matching.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Matching.Queries.GetItemMatches;

public record GetItemMatchesQuery(
    int ItemId,
    int CurrentUserId,
    bool IsAdmin
) : IRequest<Result<IReadOnlyList<MatchResultDto>>>;
