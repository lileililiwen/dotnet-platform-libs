using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Platform.Identity.Contracts;

namespace Platform.Identity.AspNetCore;

/// <summary>Projects standard claims into the provider-neutral current-user contract.</summary>
public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor, IOptions<PlatformIdentityOptions> options) : ICurrentUserAccessor
{
    /// <inheritdoc />
    public CurrentUser GetCurrentUser()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return CurrentUser.Anonymous;

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        var tenant = principal.FindFirstValue(options.Value.TenantClaimType);
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var permissions = principal.FindAll(options.Value.PermissionClaimType).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return new CurrentUser(subject, email, tenant, roles, permissions);
    }
}
