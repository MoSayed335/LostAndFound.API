using System.Text;
using System.Text.Json.Serialization;
using LostAndFound.API.Middleware;
using LostAndFound.API.Services;
using LostAndFound.Application;
using LostAndFound.Application.Interfaces;
using LostAndFound.Infrastructure;
using LostAndFound.Infrastructure.Auth;
using LostAndFound.Infrastructure.Persistence;
using LostAndFound.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

namespace LostAndFound.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console());

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication();
            //HangFireConnction
             builder.Services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                builder.Configuration.GetConnectionString("HangfireConnection")));

            builder.Services.AddHangfireServer();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                            .Where(e => e.Value?.Errors.Count > 0)
                            .SelectMany(kvp => kvp.Value!.Errors.Select(err => new
                            {
                                field = FormatFieldName(kvp.Key),
                                message = string.IsNullOrEmpty(err.ErrorMessage) ? "Invalid input value." : err.ErrorMessage
                            }))
                            .ToList();

                        var payload = new
                        {
                            success = false,
                            message = "Validation failed.",
                            errors
                        };

                        return new BadRequestObjectResult(payload);
                    };
                });

            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException("JWT configuration ('Jwt' section) is missing.");

            if (string.IsNullOrWhiteSpace(jwtOptions.Key))
            {
                throw new InvalidOperationException("JWT signing key ('Jwt:Key') is not configured.");
            }

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new()
                    {
                        Title = "Lost & Found API",
                        Version = "v1",
                        Description = "A production-grade Lost & Found RESTful API built with ASP.NET Core .NET 9 and Clean Architecture."
                    };

                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter JWT Bearer token: Bearer {your token}",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    };

                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes.Add("Bearer", securityScheme);

                    document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }] = Array.Empty<string>()
                    });

                    return Task.CompletedTask;
                });
            });

            var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "items"));
            builder.Environment.WebRootPath = webRoot;

            var app = builder.Build();

            app.UseGlobalExceptionHandling();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
                await DbInitializer.SeedAsync(scope.ServiceProvider);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.WithTitle("Lost & Found API Reference")
                           .WithTheme(ScalarTheme.Moon);
                });
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
            //HangFireDashboard

            app.MapControllers();

            app.Run();
        }

        private static string FormatFieldName(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var cleanKey = key.TrimStart('$', '.');
            if (string.IsNullOrEmpty(cleanKey)) return string.Empty;
            return char.ToLowerInvariant(cleanKey[0]) + cleanKey[1..];
        }
    }
}
