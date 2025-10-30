using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Helpers;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Unit tests for TypeHelper methods.
/// Tests all helper methods for common type checking operations.
/// </summary>
public class TypeHelperTests
{
    #region IsRequestType Tests

    [Fact]
    public void IsRequestType_WithNullTypeSymbol_ReturnsFalse()
    {
        // Act
        var result = TypeHelper.IsRequestType(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRequestType_WithIRequestImplementation_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequest {}
    public class TestRequest : IRequest {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestRequest");

        // Act
        var result = TypeHelper.IsRequestType(typeSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestType_WithGenericIRequestImplementation_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequest<T> {}
    public class TestRequest : IRequest<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestRequest");

        // Act
        var result = TypeHelper.IsRequestType(typeSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRequestType_WithNonRequestType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = TypeHelper.IsRequestType(typeSymbol);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsStreamRequestType Tests

    [Fact]
    public void IsStreamRequestType_WithNullTypeSymbol_ReturnsFalse()
    {
        // Act
        var result = TypeHelper.IsStreamRequestType(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStreamRequestType_WithIStreamRequestImplementation_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IStreamRequest<T> {}
    public class TestStreamRequest : IStreamRequest<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestStreamRequest");

        // Act
        var result = TypeHelper.IsStreamRequestType(typeSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStreamRequestType_WithNonStreamRequestType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = TypeHelper.IsStreamRequestType(typeSymbol);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNotificationType Tests

    [Fact]
    public void IsNotificationType_WithNullTypeSymbol_ReturnsFalse()
    {
        // Act
        var result = TypeHelper.IsNotificationType(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNotificationType_WithINotificationImplementation_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface INotification {}
    public class TestNotification : INotification {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestNotification");

        // Act
        var result = TypeHelper.IsNotificationType(typeSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNotificationType_WithNonNotificationType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = TypeHelper.IsNotificationType(typeSymbol);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetResponseType Tests

    [Fact]
    public void GetResponseType_WithNullRequestType_ReturnsNull()
    {
        // Act
        var result = TypeHelper.GetResponseType(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetResponseType_WithGenericIRequest_ReturnsResponseType()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequest<T> {}
    public class TestRequest : IRequest<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestRequest");

        // Act
        var result = TypeHelper.GetResponseType(typeSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("String", result!.Name);
    }

    [Fact]
    public void GetResponseType_WithGenericIStreamRequest_ReturnsResponseType()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IStreamRequest<T> {}
    public class TestStreamRequest : IStreamRequest<int> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestStreamRequest");

        // Act
        var result = TypeHelper.GetResponseType(typeSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Int32", result!.Name);
    }

    [Fact]
    public void GetResponseType_WithNonGenericRequest_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequest {}
    public class TestRequest : IRequest {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestRequest");

        // Act
        var result = TypeHelper.GetResponseType(typeSymbol);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IsValidReturnType Tests

    [Fact]
    public void IsValidReturnType_WithNullReturnType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidReturnType(null, expectedType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidReturnType_WithNullExpectedType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var returnType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidReturnType(returnType, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidReturnType_WithDirectMatch_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var stringType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidReturnType(stringType, stringType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidReturnType_WithTaskReturnType_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestClass
    {
        public Task<string> TestMethod() { return Task.FromResult(string.Empty); }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidReturnType(returnType, expectedType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidReturnType_WithValueTaskReturnType_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestClass
    {
        public ValueTask<string> TestMethod() { return ValueTask.FromResult(string.Empty); }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidReturnType(returnType, expectedType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidReturnType_WithMismatchedTypes_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var stringType = compilation.GetTypeByMetadataName("System.String");
        var intType = compilation.GetTypeByMetadataName("System.Int32");

        // Act
        var result = TypeHelper.IsValidReturnType(stringType, intType);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidVoidReturnType Tests

    [Fact]
    public void IsValidVoidReturnType_WithNullReturnType_ReturnsFalse()
    {
        // Act
        var result = TypeHelper.IsValidVoidReturnType(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVoidReturnType_WithTaskType_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        // Act
        var result = TypeHelper.IsValidVoidReturnType(taskType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidVoidReturnType_WithValueTaskType_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");

        // Act
        var result = TypeHelper.IsValidVoidReturnType(valueTaskType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidVoidReturnType_WithGenericTaskType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestClass
    {
        public Task<string> TestMethod() { return Task.FromResult(string.Empty); }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;

        // Act
        var result = TypeHelper.IsValidVoidReturnType(returnType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVoidReturnType_WithNonTaskType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var stringType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidVoidReturnType(stringType);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidStreamReturnType Tests

    [Fact]
    public void IsValidStreamReturnType_WithNullReturnType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidStreamReturnType(null, expectedType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidStreamReturnType_WithNullExpectedType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
namespace TestNamespace
{
    public class TestClass
    {
        public IAsyncEnumerable<string> TestMethod() { return null; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;

        // Act
        var result = TypeHelper.IsValidStreamReturnType(returnType, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidStreamReturnType_WithMatchingIAsyncEnumerable_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
namespace TestNamespace
{
    public class TestClass
    {
        public IAsyncEnumerable<string> TestMethod() { return null; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidStreamReturnType(returnType, expectedType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidStreamReturnType_WithMismatchedIAsyncEnumerable_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
namespace TestNamespace
{
    public class TestClass
    {
        public IAsyncEnumerable<string> TestMethod() { return null; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;
        var expectedType = compilation.GetTypeByMetadataName("System.Int32");

        // Act
        var result = TypeHelper.IsValidStreamReturnType(returnType, expectedType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidStreamReturnType_WithNonIAsyncEnumerable_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var returnType = compilation.GetTypeByMetadataName("System.String");
        var expectedType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = TypeHelper.IsValidStreamReturnType(returnType, expectedType);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetHandlerInterface Tests

    [Fact]
    public void GetHandlerInterface_WithNullHandlerType_ReturnsNull()
    {
        // Act
        var result = TypeHelper.GetHandlerInterface(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetHandlerInterface_WithIRequestHandlerImplementation_ReturnsInterface()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequestHandler {}
    public class TestHandler : IRequestHandler {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");

        // Act
        var result = TypeHelper.GetHandlerInterface(typeSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IRequestHandler", result!.Name);
    }

    [Fact]
    public void GetHandlerInterface_WithINotificationHandlerImplementation_ReturnsInterface()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface INotificationHandler {}
    public class TestHandler : INotificationHandler {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");

        // Act
        var result = TypeHelper.GetHandlerInterface(typeSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("INotificationHandler", result!.Name);
    }

    [Fact]
    public void GetHandlerInterface_WithIStreamHandlerImplementation_ReturnsInterface()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IStreamHandler {}
    public class TestHandler : IStreamHandler {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");

        // Act
        var result = TypeHelper.GetHandlerInterface(typeSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IStreamHandler", result!.Name);
    }

    [Fact]
    public void GetHandlerInterface_WithNonHandlerType_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = TypeHelper.GetHandlerInterface(typeSymbol);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IsValidHandlerSignature Tests

    [Fact]
    public void IsValidHandlerSignature_WithNullMethodSymbol_ReturnsFalse()
    {
        // Act
        var result = TypeHelper.IsValidHandlerSignature(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidHandlerSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
using System.Threading.Tasks;
namespace TestNamespace
{
    public interface IRequest {}
    public class TestRequest : IRequest {}
    public class TestHandler
    {
        public Task Handle(TestRequest request) { return Task.CompletedTask; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "Handle");

        // Act
        var result = TypeHelper.IsValidHandlerSignature(methodSymbol);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidHandlerSignature_WithNoParameters_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestHandler
    {
        public Task Handle() { return Task.CompletedTask; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "Handle");

        // Act
        var result = TypeHelper.IsValidHandlerSignature(methodSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidHandlerSignature_WithInvalidFirstParameter_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestHandler
    {
        public Task Handle(string request) { return Task.CompletedTask; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "Handle");

        // Act
        var result = TypeHelper.IsValidHandlerSignature(methodSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidHandlerSignature_WithNonTaskReturnType_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface IRequest {}
    public class TestHandler
    {
        public void Handle(IRequest request) {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestHandler");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "Handle");

        // Act
        var result = TypeHelper.IsValidHandlerSignature(methodSymbol);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetFriendlyName Tests

    [Fact]
    public void GetFriendlyName_WithNullTypeSymbol_ReturnsUnknown()
    {
        // Act
        var result = TypeHelper.GetFriendlyName(null);

        // Assert
        Assert.Equal("unknown", result);
    }

    [Fact]
    public void GetFriendlyName_WithSimpleType_ReturnsName()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act
        var result = TypeHelper.GetFriendlyName(typeSymbol);

        // Assert
        Assert.Equal("Test", result);
    }

    [Fact]
    public void GetFriendlyName_WithGenericType_ReturnsFormattedName()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
namespace TestNamespace
{
    public class TestClass
    {
        public List<string> TestMethod() { return null; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var returnType = methodSymbol!.ReturnType;

        // Act
        var result = TypeHelper.GetFriendlyName(returnType);

        // Assert
        Assert.Equal("List<String>", result);
    }

    #endregion

    #region AreEqual Tests

    [Fact]
    public void AreEqual_WithBothNull_ReturnsTrue()
    {
        // Act
        var result = TypeHelper.AreEqual(null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreEqual_WithFirstNull_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act
        var result = TypeHelper.AreEqual(null, typeSymbol);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreEqual_WithSecondNull_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act
        var result = TypeHelper.AreEqual(typeSymbol, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreEqual_WithEqualTypes_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol1 = compilation.GetTypeByMetadataName("Test");
        var typeSymbol2 = compilation.GetTypeByMetadataName("Test");

        // Act
        var result = TypeHelper.AreEqual(typeSymbol1, typeSymbol2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreEqual_WithDifferentTypes_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var stringType = compilation.GetTypeByMetadataName("System.String");
        var intType = compilation.GetTypeByMetadataName("System.Int32");

        // Act
        var result = TypeHelper.AreEqual(stringType, intType);

        // Assert
        Assert.False(result);
    }

    #endregion
}