namespace LostAndFound.Infrastructure.Persistence.Seed;

/// <summary>
/// Bound from the "SeedAdmin" configuration section (appsettings, user-secrets, or
/// environment variables). If Email/Password are left empty, seeding a dev admin
/// account is skipped - this must be explicitly configured, never hardcoded.
/// </summary>
public class AdminSeedOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Admin";
}
