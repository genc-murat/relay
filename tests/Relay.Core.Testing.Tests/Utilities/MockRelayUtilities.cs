using System.Threading.Tasks;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Utilities for creating mock Relay instances for testing
/// </summary>
public static class MockRelayUtilities
{
    /// <summary>
    /// Creates a mockable relay for testing
    /// </summary>
    public static Mock<IRelay> CreateMockRelay()
    {
        var mock = new Mock<IRelay>();

        // Set up default behaviors for common scenarios
        mock.Setup(r => r.SendAsync(It.IsAny<IRequest>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Set up generic request handling for TestRequest<T>
        mock.Setup(r => r.SendAsync(It.IsAny<TestRequest<string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.FromResult(string.Empty));

        // Set up for other specific types
        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.FromResult(string.Empty));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<int>>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.FromResult(0));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<bool>>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.FromResult(false));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<object>>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.FromResult<object>(null!));

        mock.Setup(r => r.PublishAsync(It.IsAny<INotification>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return mock;
    }
}
