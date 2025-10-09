using System.Diagnostics;
using Relay.MessageBroker;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTelemetryTests
{
    [Fact]
    public void MessageBrokerTelemetryAdapter_ShouldBeInstantiable()
    {
        // Arrange & Act
        var adapter = new MessageBrokerTelemetryAdapter(
            Microsoft.Extensions.Options.Options.Create(new UnifiedTelemetryOptions 
            { 
                Component = UnifiedTelemetryConstants.Components.MessageBroker 
            }),
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageBrokerTelemetryAdapter>());
        
        // Assert
        Assert.NotNull(adapter);
    }
}
