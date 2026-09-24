using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private const string DefaultRole = "User";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<RegisterCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registration rejected: User with email {Email} already exists.", request.Email);
            return Result<AuthResponseDto>.Failure("A user with this email already exists.", ResultErrorType.Conflict);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errorMessage = string.Join(" ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration validation failed for {Email}: {Errors}", request.Email, errorMessage);
            return Result<AuthResponseDto>.Failure(errorMessage, ResultErrorType.Validation);
        }

        await _userManager.AddToRoleAsync(user, DefaultRole);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation("User {UserId} ({Email}) registered successfully.", user.Id, user.Email);

        var response = new AuthResponseDto(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}".Trim(),
            roles.ToList(),
            token,
            expiresAtUtc);

        return Result<AuthResponseDto>.Success(response);
    }
}
