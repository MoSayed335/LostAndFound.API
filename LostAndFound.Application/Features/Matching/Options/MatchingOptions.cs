namespace LostAndFound.Application.Features.Matching.Options;

public class MatchingOptions
{
    public const string SectionName = "Matching";

    public int MinimumMatchScore { get; set; } = 40;

    public int CategoryWeight { get; set; } = 25;

    public int LocationWeight { get; set; } = 20;

    public int DateWeight { get; set; } = 20;

    public int TitleWeight { get; set; } = 20;

    public int DescriptionWeight { get; set; } = 15;
}
