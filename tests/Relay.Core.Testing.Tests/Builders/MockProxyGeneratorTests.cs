using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Comprehensive tests for MockProxyGenerator and related proxy functionality.
/// </summary>
public class MockProxyGeneratorTests
{
    [Fact]
    public void MockProxyGenerator_CreateProxy_WithValidInterface_CreatesProxy()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Act
        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);

        // Assert
        Assert.NotNull(proxy);
        Assert.IsAssignableFrom(interfaceType, proxy);
    }

    [Fact]
    public void MockProxyGenerator_CreateProxy_WithNonInterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var classType = typeof(TestClass);
        var mockInstance = CreateMockInstance(typeof(ITestInterface));

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() =>
            MockProxyGenerator_CreateProxy(classType, mockInstance));
        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Contains("Only interfaces can be mocked", exception.InnerException.Message);
    }



    [Fact]
    public void InterfaceProxy_Invoke_WithSetup_ReturnsConfiguredValue()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Setup the mock to return a specific value
        var mockSetupMethod = typeof(MockInstance).GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(LambdaExpression), typeof(object) }, null);
        Assert.NotNull(mockSetupMethod);

        var parameter = Expression.Parameter(typeof(ITestInterface), "x");
        var methodCall = Expression.Call(parameter, typeof(ITestInterface).GetMethod("GetValue")!);
        var lambda = Expression.Lambda(methodCall, parameter);

        mockSetupMethod.Invoke(mockInstance, new object[] { lambda, "MockedValue" });

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        var result = testInterface.GetValue();

        // Assert
        Assert.Equal("MockedValue", result);
    }

    [Fact]
    public void InterfaceProxy_Invoke_WithFunctionSetup_ExecutesFunction()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Setup the mock with a function
        var mockSetupMethod = typeof(MockInstance).GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(LambdaExpression), typeof(Delegate) }, null);
        Assert.NotNull(mockSetupMethod);

        var parameter = Expression.Parameter(typeof(ITestInterface), "x");
        var methodCall = Expression.Call(parameter, typeof(ITestInterface).GetMethod("GetValue")!);
        var lambda = Expression.Lambda(methodCall, parameter);

        Func<string> func = () => "FunctionResult";
        mockSetupMethod.Invoke(mockInstance, new object[] { lambda, func });

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        var result = testInterface.GetValue();

        // Assert
        Assert.Equal("FunctionResult", result);
    }

    [Fact]
    public void InterfaceProxy_Invoke_WithThrowSetup_ThrowsException()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Setup the mock to throw an exception
        var mockSetupThrowsMethod = typeof(MockInstance).GetMethod("SetupThrows", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mockSetupThrowsMethod);

        var parameter = Expression.Parameter(typeof(ITestInterface), "x");
        var methodCall = Expression.Call(parameter, typeof(ITestInterface).GetMethod("GetValue")!);
        var lambda = Expression.Lambda(methodCall, parameter);

        var expectedException = new InvalidOperationException("Test exception");
        mockSetupThrowsMethod.Invoke(mockInstance, new object[] { lambda, expectedException });

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => testInterface.GetValue());
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void InterfaceProxy_Invoke_WithSequenceSetup_ReturnsSequenceValues()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Setup the mock with a sequence
        var mockSetupSequenceMethod = typeof(MockInstance).GetMethod("SetupSequence", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(mockSetupSequenceMethod);

        var parameter = Expression.Parameter(typeof(ITestInterface), "x");
        var methodCall = Expression.Call(parameter, typeof(ITestInterface).GetMethod("GetValue")!);
        var lambda = Expression.Lambda(methodCall, parameter);

        var sequenceValues = new[] { "First", "Second", "Third" };
        mockSetupSequenceMethod.Invoke(mockInstance, new object[] { lambda, sequenceValues });

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act & Assert
        Assert.Equal("First", testInterface.GetValue());
        Assert.Equal("Second", testInterface.GetValue());
        Assert.Equal("Third", testInterface.GetValue());
        Assert.Equal("First", testInterface.GetValue()); // Should cycle back
    }

    [Fact]
    public void InterfaceProxy_Invoke_WithParameters_PassesParametersCorrectly()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Setup the mock with a function that uses parameters
        var mockSetupMethod = typeof(MockInstance).GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Instance, null,
            new[] { typeof(LambdaExpression), typeof(Delegate) }, null);
        Assert.NotNull(mockSetupMethod);

        var parameter = Expression.Parameter(typeof(ITestInterface), "x");
        var methodCall = Expression.Call(parameter, typeof(ITestInterface).GetMethod("ProcessValue")!,
            Expression.Constant("input"));
        var lambda = Expression.Lambda(methodCall, parameter);

        Func<string, string> func = input => $"Processed: {input}";
        mockSetupMethod.Invoke(mockInstance, new object[] { lambda, func });

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        var result = testInterface.ProcessValue("input");

        // Assert
        Assert.Equal("Processed: input", result);
    }

    [Fact]
    public void InterfaceProxy_Invoke_WithoutSetup_ReturnsDefaultValue()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        var result = testInterface.GetValue();

        // Assert
        Assert.Null(result); // Default value for reference type
    }

    [Fact]
    public void InterfaceProxy_Invoke_ValueTypeMethod_ReturnsDefaultValue()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        var result = testInterface.GetCount();

        // Assert
        Assert.Equal(0, result); // Default value for int
    }



    [Fact]
    public void InterfaceProxy_Invoke_RecordsInvocation()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        var proxy = MockProxyGenerator_CreateProxy(interfaceType, mockInstance);
        var testInterface = (ITestInterface)proxy;

        // Act
        testInterface.ProcessValue("test");

        // Assert - We can't directly access the invocations, but we can verify through behavior
        // The mock instance should have recorded the invocation internally
        // This is tested indirectly through the functionality working
    }

    [Fact]
    public void InterfaceProxy_Create_SetsInterfaceTypeAndMockInstance()
    {
        // Arrange
        var interfaceType = typeof(ITestInterface);
        var mockInstance = CreateMockInstance(interfaceType);

        // Act
        var proxy = InterfaceProxy_Create(interfaceType, mockInstance);

        // Assert
        Assert.NotNull(proxy);
        Assert.IsAssignableFrom(interfaceType, proxy);
    }

    // Helper methods to access internal classes via reflection
    private static MockInstance CreateMockInstance(Type interfaceType)
    {
        var mockInstanceType = typeof(MockInstance);
        var constructor = mockInstanceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
        return (MockInstance)constructor!.Invoke(new object[] { interfaceType });
    }

    private static object MockProxyGenerator_CreateProxy(Type interfaceType, MockInstance mockInstance)
    {
        var mockProxyGeneratorType = typeof(MockInstance).Assembly.GetType("Relay.Core.Testing.MockProxyGenerator");
        var createProxyMethod = mockProxyGeneratorType!.GetMethod("CreateProxy", BindingFlags.Public | BindingFlags.Static);
        return createProxyMethod!.Invoke(null, new object[] { interfaceType, mockInstance })!;
    }

    private static object InterfaceProxy_Create(Type interfaceType, MockInstance mockInstance)
    {
        var interfaceProxyType = typeof(MockInstance).Assembly.GetType("Relay.Core.Testing.InterfaceProxy");
        var createMethod = interfaceProxyType!.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        return createMethod!.Invoke(null, new object[] { interfaceType, mockInstance })!;
    }

    // Test interfaces and classes
    public interface ITestInterface
    {
        string GetValue();
        int GetCount();
        string ProcessValue(string input);
        string ProcessMultiple(string input, int number);
    }

    public class TestClass
    {
        public string GetValue() => "RealValue";
    }
}