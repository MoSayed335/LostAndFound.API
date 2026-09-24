using LostAndFound.Application.Features.Matching.DTOs;
using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces;

public interface IMatchingService
{
    IReadOnlyList<MatchResultDto> FindMatches(Item sourceItem, IEnumerable<Item> candidateItems);

    MatchResultDto? CalculateMatch(Item sourceItem, Item candidateItem);
}
