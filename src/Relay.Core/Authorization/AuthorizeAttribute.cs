using System;

namespace Relay.Core.Authorization;

/// <summary>
/// Attribute to mark handlers or requests that require authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Gets the roles required for authorization.
    /// </summary>
    public string[] Roles { get; }

    /// <summary>
    /// Gets the policies required for authorization.
    /// </summary>
    public string[] Policies { get; }

    /// <summary>
    /// Gets the authentication schemes required for authorization.
    /// </summary>
    public string[] AuthenticationSchemes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with roles.
    /// </summary>
    /// <param name="roles">The roles required for authorization.</param>
    public AuthorizeAttribute(params string[] roles)
    {
        Roles = roles ?? Array.Empty<string>();
        Policies = Array.Empty<string>();
        AuthenticationSchemes = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with policies.
    /// </summary>
    /// <param name="policies">The policies required for authorization.</param>
    public AuthorizeAttribute(bool usePolicies, params string[] policies)
    {
        if (usePolicies)
        {
            Roles = Array.Empty<string>();
            Policies = policies ?? Array.Empty<string>();
            AuthenticationSchemes = Array.Empty<string>();
        }
        else
        {
            Roles = policies ?? Array.Empty<string>();
            Policies = Array.Empty<string>();
            AuthenticationSchemes = Array.Empty<string>();
        }
    }
}