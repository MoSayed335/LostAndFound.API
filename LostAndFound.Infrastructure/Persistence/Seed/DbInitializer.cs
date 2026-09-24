using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    private static readonly string[] DefaultRoles = { "Admin", "User" };

    private static readonly string[] DefaultCategories =
    {
        "Electronics", "Wallet", "Keys", "Documents", "Bags", "Clothing", "Other"
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        await SeedRolesAsync(roleManager);
        await SeedCategoriesAsync(context);
        await SeedAdminUserAsync(userManager, configuration);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        foreach (var roleName in DefaultRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var categories = DefaultCategories.Select(name => new Category { Name = name });
        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var options = configuration.GetSection(AdminSeedOptions.SectionName).Get<AdminSeedOptions>()
                      ?? new AdminSeedOptions();

        // Dev-only convenience seed. If not configured (e.g. in Production), skip silently
        // rather than ever falling back to a hardcoded credential.
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(options.Email);
        if (existingAdmin is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.Email,
            Email = options.Email,
            FirstName = options.FirstName,
            LastName = options.LastName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        // UserManager.CreateAsync hashes the password via ASP.NET Identity's
        // configured IPasswordHasher - the plaintext value is never stored.
        var result = await userManager.CreateAsync(admin, options.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
