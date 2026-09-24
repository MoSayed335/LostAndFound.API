using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Auth.Commands.Login;
using LostAndFound.Application.Features.Auth.Commands.Register;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Auth;
using LostAndFound.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LostAndFound.Tests.Authentication;

public class AuthHandlerAndSecurityTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock = new();
    private readonly Mock<ILogger<RegisterCommandHandler>> _registerLoggerMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loginLoggerMock = new();

    public AuthHandlerAndSecurityTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingUser = UserFactory.CreateUser(1, "existing@lostandfound.dev");
        _userManagerMock.Setup(m => m.FindByEmailAsync("existing@lostandfound.dev"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _registerLoggerMock.Object);
        var command = new RegisterCommand("Jane", "Doe", "existing@lostandfound.dev", "SecurePassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Register_WhenPasswordIsWeak_ShouldReturnValidationError()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too short." }));

        var handler = new RegisterCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _registerLoggerMock.Object);
        var command = new RegisterCommand("Jane", "Doe", "new@lostandfound.dev", "weak");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Contain("Password too short");
    }

    [Fact]
    public async Task Register_WhenValid_ShouldCreateUser_AssignDefaultRole_AndReturnToken()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("mock.jwt.token", DateTime.UtcNow.AddHours(2)));

        var handler = new RegisterCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _registerLoggerMock.Object);
        var command = new RegisterCommand("Jane", "Doe", "jane@lostandfound.dev", "ValidPass123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("mock.jwt.token");
        result.Value.Email.Should().Be("jane@lostandfound.dev");
        result.Value.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync("missing@lostandfound.dev"))
            .ReturnsAsync((ApplicationUser?)null);

        var handler = new LoginCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _loginLoggerMock.Object);
        var command = new LoginCommand("missing@lostandfound.dev", "AnyPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WhenPasswordIsIncorrect_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = UserFactory.CreateUser(1, "user@lostandfound.dev");
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "WrongPassword"))
            .ReturnsAsync(false);

        var handler = new LoginCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _loginLoggerMock.Object);
        var command = new LoginCommand(user.Email!, "WrongPassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        _userManagerMock.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldResetAccessFailed_AndReturnToken()
    {
        // Arrange
        var user = UserFactory.CreateUser(1, "user@lostandfound.dev");
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "CorrectPassword123!"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        _jwtServiceMock.Setup(j => j.GenerateToken(user, It.IsAny<IEnumerable<string>>()))
            .Returns(("valid.jwt.token", DateTime.UtcNow.AddHours(2)));

        var handler = new LoginCommandHandler(_userManagerMock.Object, _jwtServiceMock.Object, _loginLoggerMock.Object);
        var command = new LoginCommand(user.Email!, "CorrectPassword123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("valid.jwt.token");
        _userManagerMock.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateValidSignedToken_WithExpectedClaims()
    {
        // Arrange
        var jwtOptions = new JwtOptions
        {
            Key = "Test_Secret_Key_For_Unit_Testing_Only_32_Bytes_Long!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };

        var service = new JwtTokenService(Options.Create(jwtOptions));
        var user = UserFactory.CreateUser(42, "tester@lostandfound.dev");
        var roles = new[] { "User", "Admin" };

        // Act
        var (tokenString, expiresAtUtc) = service.GenerateToken(user, roles);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();
        expiresAtUtc.Should().BeAfter(DateTime.UtcNow);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true
        };

        var principal = tokenHandler.ValidateToken(tokenString, validationParams, out var validatedToken);
        validatedToken.Should().NotBeNull();

        principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be("42");
        principal.FindFirst("uid")?.Value.Should().Be("42");
        principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value.Should().Be("tester@lostandfound.dev");

        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain("User");
        roleClaims.Should().Contain("Admin");
    }
}
