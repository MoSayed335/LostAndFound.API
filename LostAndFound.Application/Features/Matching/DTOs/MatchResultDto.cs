using System.Text.Json.Serialization;
using LostAndFound.Domain.Enums;

namespace LostAndFound.Application.Features.Matching.DTOs;

public class MatchResultDto
{
    public int ItemId { get; set; }

    public string Title { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ItemType Type { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime DateLostOrFound { get; set; }

    public int MatchScore { get; set; }

    public List<string> MatchingReasons { get; set; } = new();
}
