using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline.Behaviors
{
    /// <summary>
    /// Example auditor interface for demonstration purposes.
    /// </summary>
    public interface IRequestAuditor
    {
        ValueTask AuditRequestAsync(string requestType, object request, CancellationToken cancellationToken);
    }
}
