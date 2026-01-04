using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents an authorization requirement that enforces the presence of a valid share token in an authorization
/// policy.
/// </summary>
/// <remarks>Use <see cref="ShareTokenRequirement"/> in an authorization policy to restrict access to resources
/// that require a share token for authentication or sharing scenarios.</remarks>
public class ShareTokenRequirement : IAuthorizationRequirement
{
}
