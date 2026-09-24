using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.DTOs;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed for email: {Email}. User not found.", request.Email);
            return Result<AuthResponseDto>.Failure(InvalidCredentialsMessage, ResultErrorType.Unauthorized);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed for email: {Email}. Account is locked out.", request.Email);
            return Result<AuthResponseDto>.Failure(
                "Account is temporarily locked due to multiple failed login attempts.",
                ResultErrorType.Unauthorized);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Login failed for email: {Email}. Invalid password.", request.Email);
            return Result<AuthResponseDto>.Failure(InvalidCredentialsMessage, ResultErrorType.Unauthorized);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation("User {UserId} ({Email}) logged in successfully.", user.Id, user.Email);

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
