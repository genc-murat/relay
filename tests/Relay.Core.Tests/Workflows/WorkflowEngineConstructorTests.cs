using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Relay.Core.Workflows;
using Relay.Core.Contracts.Core;

namespace Relay.Core.Tests.Workflows;

[Collection("Sequential")]
public class WorkflowEngineConstructorTests
{
    [Fact]
    public void Constructor_WithNullRelay_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(null!, Mock.Of<ILogger<WorkflowEngine>>(), Mock.Of<IWorkflowStateStore>(), Mock.Of<IWorkflowDefinitionStore>()));
        Assert.Equal("relay", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(Mock.Of<IRelay>(), null!, Mock.Of<IWorkflowStateStore>(), Mock.Of<IWorkflowDefinitionStore>()));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStateStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(Mock.Of<IRelay>(), Mock.Of<ILogger<WorkflowEngine>>(), null!, Mock.Of<IWorkflowDefinitionStore>()));
        Assert.Equal("stateStore", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDefinitionStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(Mock.Of<IRelay>(), Mock.Of<ILogger<WorkflowEngine>>(), Mock.Of<IWorkflowStateStore>(), null!));
        Assert.Equal("definitionStore", exception.ParamName);
    }
}