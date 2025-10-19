using System.Collections.Generic;
using System.Security.Claims;

namespace Relay.Core.Security
{
    /// <summary>
    /// Security context interface for accessing current user information.
    /// </summary>
    public interface ISecurityContext
    {
        string UserId { get; }
        IEnumerable<string> Roles { get; }
        IEnumerable<Claim> Claims { get; }
        bool IsAuthenticated { get; }
        bool HasPermission(string permission);
        bool HasPermissions(IEnumerable<string> permissions);
    }
}