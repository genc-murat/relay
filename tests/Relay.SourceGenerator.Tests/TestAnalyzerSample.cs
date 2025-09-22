extern alias RelayCore;

namespace Relay.SourceGenerator.Tests
{
    // Test classes for analyzer validation

    public class ValidTestRequest : RelayCore::Relay.Core.IRequest<string> { }
    public class ValidTestNotification : RelayCore::Relay.Core.INotification { }

    public class ValidTestHandler
    {
        [RelayCore::Relay.Core.Handle]
        public ValueTask<string> HandleValidRequest(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test");
        }

        [RelayCore::Relay.Core.Notification]
        public ValueTask HandleValidNotification(ValidTestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    // Invalid handlers for testing analyzer diagnostics
    public class InvalidTestHandler
    {
        // Missing request parameter
        [RelayCore::Relay.Core.Handle]
        public ValueTask<string> HandleMissingParameter()
        {
            return ValueTask.FromResult("test");
        }

        // Invalid return type
        [RelayCore::Relay.Core.Handle]
        public string HandleInvalidReturnType(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return "test";
        }

        // Missing CancellationToken (should produce warning)
        [RelayCore::Relay.Core.Handle]
        public ValueTask<string> HandleMissingCancellationToken(ValidTestRequest request)
        {
            return ValueTask.FromResult("test");
        }
    }

    // Duplicate handlers
    public class DuplicateTestHandler1
    {
        [RelayCore::Relay.Core.Handle]
        public ValueTask<string> HandleDuplicate(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test1");
        }
    }

    public class DuplicateTestHandler2
    {
        [RelayCore::Relay.Core.Handle]
        public ValueTask<string> HandleDuplicate(ValidTestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("test2");
        }
    }
}