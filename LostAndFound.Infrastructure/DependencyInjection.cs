using LostAndFound.Application.Common.Options;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Auth;
using LostAndFound.Infrastructure.Persistence;
using LostAndFound.Infrastructure.Persistence.Repositories;
using LostAndFound.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace LostAndFound.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                // Keep simple but reasonable defaults for a portfolio project
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;

                options.User.RequireUniqueEmail = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Repositories & Unit of Work
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Auth
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // File Storage
        services.Configure<ImageUploadOptions>(configuration.GetSection(ImageUploadOptions.SectionName));
        services.AddScoped<IFileService, FileService>();

        // Background Processing & Notifications
        services.AddScoped<INotificationService, EmailNotificationService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        return services;
    }
}
