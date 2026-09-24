using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LostAndFound.Application.Interfaces;

namespace LostAndFound.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var user = User;
            if (user is null) return null;

            var claim = user.FindFirst("uid")
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst(JwtRegisteredClaimNames.Sub);

            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value ??
        User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => User?.IsInRole("Admin") ?? false;
}
