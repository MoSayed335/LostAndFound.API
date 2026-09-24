using LostAndFound.Domain.Enums;

namespace LostAndFound.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType Type { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Location { get; set; } = string.Empty;
    public DateTime DateLostOrFound { get; set; }
    public string? ImageUrl { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
