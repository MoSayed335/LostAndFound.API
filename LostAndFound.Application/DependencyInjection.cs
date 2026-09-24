using System.Reflection;
using FluentValidation;
using LostAndFound.Application.Common.Behaviors;
using LostAndFound.Application.Features.Matching.Options;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton(new MatchingOptions());
        services.AddScoped<IMatchingService, MatchingService>();

        return services;
    }
}

