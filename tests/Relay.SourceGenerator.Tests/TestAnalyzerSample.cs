extern alias Core;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.SourceGenerator.Tests
{
    // Test classes for analyzer validation

    public class ValidTestRequest : Core.IRequest<string> { }
    public class ValidTestNotification : Core.INotification { }

    public class ValidTestHandler
    {
        [Core.Handle]
        public ValueTask<string> HandleValidRequest(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test");
        }

        [Core.Notification]
        public ValueTask HandleValidNotification(ValidTestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    // Invalid handlers for testing analyzer diagnostics
    public class InvalidTestHandler
    {
        // Missing request parameter
        [Core.Handle]
        public ValueTask<string> HandleMissingParameter()
        {
            return ValueTask.FromResult("test");
        }

        // Invalid return type
        [Core.Handle]
        public string HandleInvalidReturnType(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return "test";
        }

        // Missing CancellationToken (should produce warning)
        [Core.Handle]
        public ValueTask<string> HandleMissingCancellationToken(ValidTestRequest request)
        {
            return ValueTask.FromResult("test");
        }
    }

    // Duplicate handlers
    public class DuplicateTestHandler1
    {
        [Core.Handle]
        public ValueTask<string> HandleDuplicate(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test1");
        }
    }

    public class DuplicateTestHandler2
    {
        [Core.Handle]
        public ValueTask<string> HandleDuplicate(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test2");
        }
    }
}