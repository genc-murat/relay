using System;
using System.Collections.Generic;

namespace Relay.Core.Security.Exceptions
{
    /// <summary>
    /// Exception thrown when user lacks required permissions.
    /// </summary>
    public class InsufficientPermissionsException : Exception
    {
        public string RequestType { get; }
        public IEnumerable<string> RequiredPermissions { get; }

        public InsufficientPermissionsException(string requestType, IEnumerable<string> requiredPermissions)
            : base($"Insufficient permissions for {requestType}. Required: {string.Join(", ", requiredPermissions)}")
        {
            RequestType = requestType;
            RequiredPermissions = requiredPermissions;
        }
    }
}