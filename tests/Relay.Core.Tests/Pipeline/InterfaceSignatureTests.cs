using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class InterfaceSignatureTests
{
    [Fact]
    public void IPipelineBehavior_Should_Have_Correct_Signature()
    {
        // Arrange & Act
        var interfaceType = typeof(IPipelineBehavior<,>);
        var method = interfaceType.GetMethod("HandleAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(ValueTask<>).Name, method.ReturnType.Name);
        Assert.Equal(3, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal("next", parameters[1].Name);
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [Fact]
    public void IStreamPipelineBehavior_Should_Have_Correct_Signature()
    {
        // Arrange & Act
        var interfaceType = typeof(IStreamPipelineBehavior<,>);
        var method = interfaceType.GetMethod("HandleAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<>).Name, method.ReturnType.Name);
        Assert.Equal(3, method.GetParameters().Length);

        var parameters = method.GetParameters();
        Assert.Equal("request", parameters[0].Name);
        Assert.Equal("next", parameters[1].Name);
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [Fact]
    public void ISystemModule_Should_Have_Correct_Properties_And_Methods()
    {
        // Arrange & Act
        var interfaceType = typeof(ISystemModule);
        var orderProperty = interfaceType.GetProperty("Order");
        var executeMethod = interfaceType.GetMethod("ExecuteAsync");
        var executeStreamMethod = interfaceType.GetMethod("ExecuteStreamAsync");

        // Assert
        Assert.NotNull(orderProperty);
        Assert.Equal(typeof(int), orderProperty.PropertyType);
        Assert.True(orderProperty.CanRead);

        Assert.NotNull(executeMethod);
        Assert.True(executeMethod.IsGenericMethodDefinition);
        Assert.Equal(2, executeMethod.GetGenericArguments().Length);

        Assert.NotNull(executeStreamMethod);
        Assert.True(executeStreamMethod.IsGenericMethodDefinition);
        Assert.Equal(2, executeStreamMethod.GetGenericArguments().Length);
    }

    [Fact]
    public void RequestHandlerDelegate_Should_Return_ValueTask()
    {
        // Arrange & Act
        var delegateType = typeof(RequestHandlerDelegate<>);
        var invokeMethod = delegateType.GetMethod("Invoke");

        // Assert
        Assert.NotNull(invokeMethod);
        Assert.Equal(typeof(ValueTask<>).Name, invokeMethod.ReturnType.Name);
        Assert.Empty(invokeMethod.GetParameters());
    }

    [Fact]
    public void StreamHandlerDelegate_Should_Return_IAsyncEnumerable()
    {
        // Arrange & Act
        var delegateType = typeof(StreamHandlerDelegate<>);
        var invokeMethod = delegateType.GetMethod("Invoke");

        // Assert
        Assert.NotNull(invokeMethod);
        Assert.Equal(typeof(IAsyncEnumerable<>).Name, invokeMethod.ReturnType.Name);
        Assert.Empty(invokeMethod.GetParameters());
    }
}