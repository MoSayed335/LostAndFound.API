using LostAndFound.Domain.Enums;

namespace LostAndFound.Domain.Entities;

public class Claim
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int ClaimantId { get; set; }
    public ApplicationUser Claimant { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
