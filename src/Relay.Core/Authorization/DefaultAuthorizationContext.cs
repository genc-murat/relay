using System.Collections.Generic;
using System.Security.Claims;

namespace Relay.Core.Authorization;

/// <summary>
/// Default implementation of IAuthorizationContext.
/// </summary>
public class DefaultAuthorizationContext : IAuthorizationContext
{
    /// <inheritdoc />
    public IEnumerable<Claim> UserClaims { get; set; } = new List<Claim>();

    /// <inheritdoc />
    public IEnumerable<string> UserRoles { get; set; } = new List<string>();

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}