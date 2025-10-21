using Relay.Core.AI.Optimization.Connection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class LoadBalancerComponentTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var component = new LoadBalancerComponent();

        // Assert
        Assert.Equal(string.Empty, component.Name);
        Assert.Equal(0, component.Count);
        Assert.Equal(string.Empty, component.Description);
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Name = "TestComponent";
        component.Count = 42;
        component.Description = "Test description";

        // Assert
        Assert.Equal("TestComponent", component.Name);
        Assert.Equal(42, component.Count);
        Assert.Equal("Test description", component.Description);
    }

    [Fact]
    public void Properties_Should_Support_Negative_Count()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Count = -5;

        // Assert
        Assert.Equal(-5, component.Count);
    }

    [Fact]
    public void Properties_Should_Support_Zero_Count()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Count = 0;

        // Assert
        Assert.Equal(0, component.Count);
    }

    [Fact]
    public void Properties_Should_Support_Large_Count()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Count = int.MaxValue;

        // Assert
        Assert.Equal(int.MaxValue, component.Count);
    }

    [Fact]
    public void Name_Property_Should_Support_Null_Value()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Name = null!;

        // Assert
        Assert.Null(component.Name);
    }

    [Fact]
    public void Description_Property_Should_Support_Null_Value()
    {
        // Arrange
        var component = new LoadBalancerComponent();

        // Act
        component.Description = null!;

        // Assert
        Assert.Null(component.Description);
    }

    [Fact]
    public void Properties_Should_Be_Independent()
    {
        // Arrange
        var component1 = new LoadBalancerComponent
        {
            Name = "Component1",
            Count = 10,
            Description = "Description1"
        };

        var component2 = new LoadBalancerComponent
        {
            Name = "Component2",
            Count = 20,
            Description = "Description2"
        };

        // Assert
        Assert.Equal("Component1", component1.Name);
        Assert.Equal(10, component1.Count);
        Assert.Equal("Description1", component1.Description);

        Assert.Equal("Component2", component2.Name);
        Assert.Equal(20, component2.Count);
        Assert.Equal("Description2", component2.Description);
    }

    [Fact]
    public void Object_Initializer_Should_Work_With_All_Properties()
    {
        // Act
        var component = new LoadBalancerComponent
        {
            Name = "HealthCheck",
            Count = 5,
            Description = "Health check and monitoring connections"
        };

        // Assert
        Assert.Equal("HealthCheck", component.Name);
        Assert.Equal(5, component.Count);
        Assert.Equal("Health check and monitoring connections", component.Description);
    }

    [Fact]
    public void Object_Initializer_Should_Work_With_Partial_Properties()
    {
        // Act
        var component = new LoadBalancerComponent
        {
            Name = "TestComponent",
            Count = 100
            // Description is not set
        };

        // Assert
        Assert.Equal("TestComponent", component.Name);
        Assert.Equal(100, component.Count);
        Assert.Equal(string.Empty, component.Description); // Default value
    }

    [Fact]
    public void Should_Support_LoadBalancer_Component_Types_From_HttpConnectionMetricsProvider()
    {
        // Test all the component types used in HttpConnectionMetricsProvider
        var components = new[]
        {
            new LoadBalancerComponent { Name = "HealthCheck", Count = 2, Description = "Health check and monitoring connections" },
            new LoadBalancerComponent { Name = "Persistent", Count = 10, Description = "Persistent load balancer communication" },
            new LoadBalancerComponent { Name = "SessionAffinity", Count = 15, Description = "Sticky session/affinity connections" },
            new LoadBalancerComponent { Name = "BackendPool", Count = 8, Description = "Connection to backend service pool" },
            new LoadBalancerComponent { Name = "Telemetry", Count = 3, Description = "Metrics reporting to LB" },
            new LoadBalancerComponent { Name = "ServiceMesh", Count = 6, Description = "Service mesh sidecar connections" }
        };

        // Assert
        Assert.Equal(6, components.Length);
        Assert.Contains(components, c => c.Name == "HealthCheck" && c.Count == 2);
        Assert.Contains(components, c => c.Name == "Persistent" && c.Count == 10);
        Assert.Contains(components, c => c.Name == "SessionAffinity" && c.Count == 15);
        Assert.Contains(components, c => c.Name == "BackendPool" && c.Count == 8);
        Assert.Contains(components, c => c.Name == "Telemetry" && c.Count == 3);
        Assert.Contains(components, c => c.Name == "ServiceMesh" && c.Count == 6);
    }

    [Fact]
    public void Should_Calculate_Sum_Of_Counts_For_LoadBalancer_Components()
    {
        // Arrange - Simulate what HttpConnectionMetricsProvider does
        var lbComponents = new List<LoadBalancerComponent>
        {
            new LoadBalancerComponent { Name = "HealthCheck", Count = 2, Description = "Health check connections" },
            new LoadBalancerComponent { Name = "Persistent", Count = 10, Description = "Persistent connections" },
            new LoadBalancerComponent { Name = "SessionAffinity", Count = 15, Description = "Affinity connections" }
        };

        // Act
        var totalConnections = lbComponents.Sum(c => c.Count);

        // Assert
        Assert.Equal(27, totalConnections);
    }

    [Fact]
    public void Properties_Should_Support_Very_Long_Strings()
    {
        // Arrange
        var longName = new string('A', 1000);
        var longDescription = new string('B', 2000);

        // Act
        var component = new LoadBalancerComponent
        {
            Name = longName,
            Description = longDescription,
            Count = 42
        };

        // Assert
        Assert.Equal(longName, component.Name);
        Assert.Equal(longDescription, component.Description);
        Assert.Equal(42, component.Count);
    }

    [Fact]
    public void Should_Support_Empty_Object_Initializer()
    {
        // Act
        var component = new LoadBalancerComponent { };

        // Assert
        Assert.Equal(string.Empty, component.Name);
        Assert.Equal(0, component.Count);
        Assert.Equal(string.Empty, component.Description);
    }
}