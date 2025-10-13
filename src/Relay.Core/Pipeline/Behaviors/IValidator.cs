using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline.Behaviors
{
    /// <summary>
    /// Example validator interface for demonstration purposes.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public interface IValidator<in T>
    {
        ValueTask ValidateAsync(T instance, CancellationToken cancellationToken);
    }
}
