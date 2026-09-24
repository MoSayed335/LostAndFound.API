using System.Text.RegularExpressions;
using LostAndFound.Application.Features.Matching.DTOs;
using LostAndFound.Application.Features.Matching.Options;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;

namespace LostAndFound.Application.Services;

public partial class MatchingService : IMatchingService
{
    private readonly MatchingOptions _options;

    public MatchingService(MatchingOptions? options = null)
    {
        _options = options ?? new MatchingOptions();
    }

    public IReadOnlyList<MatchResultDto> FindMatches(Item sourceItem, IEnumerable<Item> candidateItems)
    {
        var matches = new List<MatchResultDto>();

        foreach (var candidate in candidateItems)
        {
            var match = CalculateMatch(sourceItem, candidate);
            if (match is not null && match.MatchScore >= _options.MinimumMatchScore)
            {
                matches.Add(match);
            }
        }

        return matches
            .OrderByDescending(m => m.MatchScore)
            .ThenByDescending(m => m.DateLostOrFound)
            .ToList();
    }

    public MatchResultDto? CalculateMatch(Item sourceItem, Item candidateItem)
    {
        // 1. Must be opposite item types (Lost <-> Found only)
        if (sourceItem.Type == candidateItem.Type)
        {
            return null;
        }

        // 2. Candidate must be Active
        if (candidateItem.Status != ItemStatus.Active)
        {
            return null;
        }

        // Candidate cannot be the exact same item
        if (sourceItem.Id == candidateItem.Id)
        {
            return null;
        }

        int totalScore = 0;
        var reasons = new List<string>();

        // 3. Category matching (25 points)
        if (sourceItem.CategoryId == candidateItem.CategoryId)
        {
            totalScore += _options.CategoryWeight;
            reasons.Add("Same category");
        }

        // 4. Location matching (20 points)
        var (locationScore, locationReason) = EvaluateLocation(sourceItem.Location, candidateItem.Location);
        if (locationScore > 0)
        {
            totalScore += locationScore;
            if (!string.IsNullOrEmpty(locationReason))
            {
                reasons.Add(locationReason);
            }
        }

        // 5. Date matching (20 points)
        var (dateScore, dateReason) = EvaluateDate(sourceItem.DateLostOrFound, candidateItem.DateLostOrFound);
        if (dateScore > 0)
        {
            totalScore += dateScore;
            if (!string.IsNullOrEmpty(dateReason))
            {
                reasons.Add(dateReason);
            }
        }

        // 6. Title matching (20 points)
        var (titleScore, titleReason) = EvaluateTextSimilarity(sourceItem.Title, candidateItem.Title, _options.TitleWeight, "title");
        if (titleScore > 0)
        {
            totalScore += titleScore;
            if (!string.IsNullOrEmpty(titleReason))
            {
                reasons.Add(titleReason);
            }
        }

        // 7. Description matching (15 points)
        if (!string.IsNullOrWhiteSpace(sourceItem.Description) && !string.IsNullOrWhiteSpace(candidateItem.Description))
        {
            var (descScore, descReason) = EvaluateTextSimilarity(sourceItem.Description, candidateItem.Description, _options.DescriptionWeight, "description");
            if (descScore > 0)
            {
                totalScore += descScore;
                if (!string.IsNullOrEmpty(descReason))
                {
                    reasons.Add(descReason);
                }
            }
        }

        // Bound total score between 0 and 100
        totalScore = Math.Clamp(totalScore, 0, 100);

        return new MatchResultDto
        {
            ItemId = candidateItem.Id,
            Title = candidateItem.Title,
            Type = candidateItem.Type,
            CategoryName = candidateItem.Category?.Name ?? string.Empty,
            Location = candidateItem.Location,
            DateLostOrFound = candidateItem.DateLostOrFound,
            MatchScore = totalScore,
            MatchingReasons = reasons
        };
    }

    private (int Score, string? Reason) EvaluateLocation(string? loc1, string? loc2)
    {
        string norm1 = Normalize(loc1);
        string norm2 = Normalize(loc2);

        if (string.IsNullOrWhiteSpace(norm1) || string.IsNullOrWhiteSpace(norm2))
        {
            return (0, null);
        }

        // Exact normalized match
        if (string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase))
        {
            return (_options.LocationWeight, "Same location");
        }

        // Partial match: one string contains the other
        if (norm1.Contains(norm2, StringComparison.OrdinalIgnoreCase) || norm2.Contains(norm1, StringComparison.OrdinalIgnoreCase))
        {
            int partialScore = (int)Math.Round(_options.LocationWeight * 0.7); // 14 points out of 20
            return (partialScore, "Similar location");
        }

        // Word token overlap for location
        var tokens1 = Tokenize(norm1);
        var tokens2 = Tokenize(norm2);
        if (tokens1.Count > 0 && tokens2.Count > 0)
        {
            int common = tokens1.Intersect(tokens2).Count();
            int minTokens = Math.Min(tokens1.Count, tokens2.Count);
            if (minTokens > 0 && (double)common / minTokens >= 0.5)
            {
                int partialScore = (int)Math.Round(_options.LocationWeight * 0.6); // 12 points out of 20
                return (partialScore, "Similar location");
            }
        }

        return (0, null);
    }

    private (int Score, string? Reason) EvaluateDate(DateTime date1, DateTime date2)
    {
        int daysDiff = Math.Abs((date1.Date - date2.Date).Days);

        return daysDiff switch
        {
            0 => (20, "Same date"),
            1 => (18, "Date difference: 1 day"),
            2 => (15, "Date difference: 2 days"),
            3 => (12, "Date difference: 3 days"),
            4 => (9, "Date difference: 4 days"),
            5 => (6, "Date difference: 5 days"),
            6 => (3, "Date difference: 6 days"),
            _ => (0, null)
        };
    }

    private (int Score, string? Reason) EvaluateTextSimilarity(string? text1, string? text2, int maxWeight, string fieldLabel)
    {
        var tokens1 = Tokenize(text1);
        var tokens2 = Tokenize(text2);

        if (tokens1.Count == 0 || tokens2.Count == 0)
        {
            return (0, null);
        }

        int intersectionCount = tokens1.Intersect(tokens2).Count();
        if (intersectionCount == 0)
        {
            return (0, null);
        }

        int unionCount = tokens1.Union(tokens2).Count();
        double jaccard = unionCount > 0 ? (double)intersectionCount / unionCount : 0.0;
        double containment = (double)intersectionCount / Math.Min(tokens1.Count, tokens2.Count);

        // Weighted blend favoring matching tokens
        double similarity = (jaccard * 0.6) + (containment * 0.4);
        int score = (int)Math.Round(similarity * maxWeight);
        score = Math.Clamp(score, 0, maxWeight);

        // Minimum threshold to generate reason
        int minScoreForReason = (int)Math.Ceiling(maxWeight * 0.25);
        if (score >= minScoreForReason)
        {
            string reason = (similarity >= 0.95 && fieldLabel == "title")
                ? "Matching title"
                : $"Similar {fieldLabel}";

            return (score, reason);
        }

        return (score, null);
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string lower = text.Trim().ToLowerInvariant();
        return PunctuationRegex().Replace(lower, " ");
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>();
        }

        string normalized = Normalize(text);
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return words
            .Where(w => w.Length > 1) // filter out 1-char noise
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationRegex();
}
