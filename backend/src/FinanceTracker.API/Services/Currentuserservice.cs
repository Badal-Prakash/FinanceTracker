using FinanceTracker.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FinanceTracker.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var id = User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
    }

    public Guid TenantId
    {
        get
        {
            var id = User?.FindFirst("TenantId")?.Value;
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
    }

    public string Email => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

    public string Role => User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}