using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Domain.Entities;

/// <summary>
/// Application user. Email, PasswordHash, PhoneNumber, etc. are already
/// provided by IdentityUser&lt;int&gt; - do not duplicate them here.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
