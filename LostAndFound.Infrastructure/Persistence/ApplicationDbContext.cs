using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Persistence;

/// <summary>
/// Note: Domain.Entities.Claim (a claim submitted on an item) is a different type
/// from System.Security.Claims.Claim (an identity claim). Since this file does not
/// "using System.Security.Claims", there is no ambiguity here - but keep that in mind
/// in any file that needs both (e.g. JWT token generation in Phase 3).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Applies every IEntityTypeConfiguration<T> in this assembly (see Persistence/Configurations)
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
