namespace Platform.Identity.AspNetCore;

/// <summary>Explicit authentication scheme ownership for platform wiring.</summary>
public sealed class PlatformIdentityOptions
{
    /// <summary>Default scheme used by platform authentication registration.</summary>
    public string AuthenticationScheme { get; set; } = "PlatformIdentity";
    /// <summary>Claim type containing permission keys.</summary>
    public string PermissionClaimType { get; set; } = "permission";
    /// <summary>Claim type containing tenant identity.</summary>
    public string TenantClaimType { get; set; } = "tenant_id";
    /// <summary>Validates required options.</summary>
    public bool IsValid() => !string.IsNullOrWhiteSpace(AuthenticationScheme)
        && !string.IsNullOrWhiteSpace(PermissionClaimType)
        && !string.IsNullOrWhiteSpace(TenantClaimType);
}
