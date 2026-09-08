using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Platform.Authorization;
using Platform.Identity.Contracts;

namespace Platform.Identity.AspNetCore;

internal sealed class PlatformPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

internal sealed class PlatformPermissionHandler(ICurrentUserAccessor currentUser, IAuthorizationDecisionAuditor? auditor = null)
    : AuthorizationHandler<PlatformPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformPermissionRequirement requirement)
    {
        var user = currentUser.GetCurrentUser();
        var granted = user.IsAuthenticated && user.PermissionSet.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase);
        if (granted)
            context.Succeed(requirement);

        if (auditor is not null)
            await auditor.RecordAsync(granted
                ? AuthorizationDecision.Granted(requirement.Permission)
                : AuthorizationDecision.Denied(requirement.Permission), user.SubjectId).ConfigureAwait(false);
    }
}

/// <summary>Registration and policy helpers for platform identity.</summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>Registers current-user projection and ASP.NET authorization helpers.</summary>
    public static IServiceCollection AddPlatformIdentity(this IServiceCollection services) => services.AddPlatformIdentity(_ => { });

    /// <summary>Registers platform identity services with explicit options.</summary>
    public static IServiceCollection AddPlatformIdentity(this IServiceCollection services, Action<PlatformIdentityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<PlatformIdentityOptions>().Configure(configure).Validate(o => o.IsValid(), "Platform identity options are invalid.");
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IAuthorizationHandler, PlatformPermissionHandler>());
        return services;
    }

    /// <summary>Registers the platform-owned authentication scheme without replacing consumer schemes.</summary>
    public static AuthenticationBuilder AddPlatformIdentityAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new PlatformIdentityOptions();
        return services.AddAuthentication(authentication =>
        {
            authentication.DefaultAuthenticateScheme = options.AuthenticationScheme;
            authentication.DefaultChallengeScheme = options.AuthenticationScheme;
        });
    }

    /// <summary>Adds a named policy requiring the given module-owned permission.</summary>
    public static IServiceCollection RequirePlatformPermission(this IServiceCollection services, string permissionKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(permissionKey)) throw new ArgumentException("Permission is required.", nameof(permissionKey));
        services.AddAuthorizationBuilder().AddPolicy(PlatformPolicyNames.ForPermission(permissionKey), policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new PlatformPermissionRequirement(permissionKey)));
        return services;
    }

    /// <summary>Adds a named policy requiring an application-owned role.</summary>
    public static IServiceCollection RequirePlatformRole(this IServiceCollection services, string role)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));
        services.AddAuthorizationBuilder().AddPolicy(PlatformPolicyNames.ForRole(role), policy =>
            policy.RequireAuthenticatedUser().RequireRole(role));
        return services;
    }

    /// <summary>Returns a policy name for use with endpoint or controller authorization.</summary>
    public static string PlatformPermissionPolicy(string permissionKey) => PlatformPolicyNames.ForPermission(permissionKey);

    /// <summary>Returns a policy name for use with endpoint or controller authorization.</summary>
    public static string PlatformRolePolicy(string role) => PlatformPolicyNames.ForRole(role);
}
