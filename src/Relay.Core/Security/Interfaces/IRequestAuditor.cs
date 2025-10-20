using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Security.Interfaces
{
    /// <summary>
    /// Request auditing interface.
    /// </summary>
    public interface IRequestAuditor
    {
        ValueTask LogRequestAsync(string userId, string requestType, object request, CancellationToken cancellationToken);
        ValueTask LogSuccessAsync(string userId, string requestType, CancellationToken cancellationToken);
        ValueTask LogFailureAsync(string userId, string requestType, Exception exception, CancellationToken cancellationToken);
    }
}