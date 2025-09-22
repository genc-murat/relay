using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Authorization
{
    /// <summary>
    /// Interface for authorization contexts.
    /// </summary>
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Gets the user's claims.
        /// </summary>
        IEnumerable<Claim> UserClaims { get; }

        /// <summary>
        /// Gets the user's roles.
        /// </summary>
        IEnumerable<string> UserRoles { get; }

        /// <summary>
        /// Gets custom properties for authorization.
        /// </summary>
        IDictionary<string, object> Properties { get; }
    }
}