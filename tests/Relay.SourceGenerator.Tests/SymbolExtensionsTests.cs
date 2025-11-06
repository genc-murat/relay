using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Extensions;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Unit tests for SymbolExtensions methods.
/// Tests all extension methods for Roslyn symbols.
/// </summary>
public class SymbolExtensionsTests
{
    #region ImplementsInterface Tests

    [Fact]
    public void ImplementsInterface_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).ImplementsInterface("ITest"));
    }

    [Fact]
    public void ImplementsInterface_WithNullInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.ImplementsInterface(null!));
    }

    [Fact]
    public void ImplementsInterface_WithEmptyInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.ImplementsInterface(""));
    }

    [Fact]
    public void ImplementsInterface_WithWhitespaceInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.ImplementsInterface("   "));
    }

    [Fact]
    public void ImplementsInterface_WithImplementedInterface_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface ITestInterface {}
    public class TestClass : ITestInterface {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.ImplementsInterface("ITestInterface");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ImplementsInterface_WithNonImplementedInterface_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface ITestInterface {}
    public interface IOtherInterface {}
    public class TestClass : ITestInterface {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.ImplementsInterface("IOtherInterface");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ImplementsInterface_WithGenericInterface_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
namespace TestNamespace
{
    public interface ITestInterface<T> {}
    public class TestClass : ITestInterface<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.ImplementsInterface("ITestInterface");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ImplementsInterface with TypeArgumentCount Tests

    [Fact]
    public void ImplementsInterface_WithTypeArgumentCount_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).ImplementsInterface("ITest", 1));
    }

    [Fact]
    public void ImplementsInterface_WithTypeArgumentCount_WithNullInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.ImplementsInterface(null!, 1));
    }

    [Fact]
    public void ImplementsInterface_WithTypeArgumentCount_WithMatchingGenericInterface_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
namespace TestNamespace
{
    public interface ITestInterface<T> {}
    public class TestClass : ITestInterface<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.ImplementsInterface("ITestInterface", 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ImplementsInterface_WithTypeArgumentCount_WithWrongTypeArgumentCount_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
namespace TestNamespace
{
    public interface ITestInterface<T> {}
    public interface ITestInterface<T1, T2> {}
    public class TestClass : ITestInterface<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.ImplementsInterface("ITestInterface", 2);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetInterface Tests

    [Fact]
    public void GetInterface_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).GetInterface("ITest"));
    }

    [Fact]
    public void GetInterface_WithNullInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.GetInterface(null!));
    }

    [Fact]
    public void GetInterface_WithImplementedInterface_ReturnsInterfaceSymbol()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface ITestInterface {}
    public class TestClass : ITestInterface {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.GetInterface("ITestInterface");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ITestInterface", result!.Name);
    }

    [Fact]
    public void GetInterface_WithNonImplementedInterface_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public interface ITestInterface {}
    public interface IOtherInterface {}
    public class TestClass : ITestInterface {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.GetInterface("IOtherInterface");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetInterface with TypeArgumentCount Tests

    [Fact]
    public void GetInterface_WithTypeArgumentCount_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).GetInterface("ITest", 1));
    }

    [Fact]
    public void GetInterface_WithTypeArgumentCount_WithNullInterfaceName_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.GetInterface(null!, 1));
    }

    [Fact]
    public void GetInterface_WithTypeArgumentCount_WithMatchingGenericInterface_ReturnsInterfaceSymbol()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
namespace TestNamespace
{
    public interface ITestInterface<T> {}
    public class TestClass : ITestInterface<string> {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.GetInterface("ITestInterface", 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ITestInterface", result!.Name);
        Assert.Equal(1, result.TypeArguments.Length);
    }

    #endregion

    #region HasAttribute Tests

    [Fact]
    public void HasAttribute_WithNullMethodSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IMethodSymbol)null!).HasAttribute("Test"));
    }

    [Fact]
    public void HasAttribute_WithNullAttributeName_ThrowsArgumentNullException()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => methodSymbol!.HasAttribute(null!));
    }

    [Fact]
    public void HasAttribute_WithAttributePresent_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    [Obsolete]
    public class TestClass
    {
        [Obsolete]
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.HasAttribute("Obsolete");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAttribute_WithAttributeNotPresent_ReturnsFalse()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.HasAttribute("Obsolete");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAttribute_WithAttributeNameWithoutSuffix_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    [Obsolete]
    public class TestClass
    {
        [Obsolete]
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.HasAttribute("Obsolete");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetAttribute Tests

    [Fact]
    public void GetAttribute_WithNullMethodSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IMethodSymbol)null!).GetAttribute("Test"));
    }

    [Fact]
    public void GetAttribute_WithNullAttributeName_ThrowsArgumentNullException()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => methodSymbol!.GetAttribute(null!));
    }

    [Fact]
    public void GetAttribute_WithAttributePresent_ReturnsAttributeData()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    [Obsolete]
    public class TestClass
    {
        [Obsolete]
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.GetAttribute("Obsolete");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ObsoleteAttribute", result!.AttributeClass!.Name);
    }

    [Fact]
    public void GetAttribute_WithAttributeNotPresent_ReturnsNull()
    {
        // Arrange
        var source = @"
using System;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.GetAttribute("Obsolete");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IsTaskType Tests

    [Fact]
    public void IsTaskType_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).IsTaskType());
    }

    [Fact]
    public void IsTaskType_WithTaskType_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        // Act
        var result = taskType!.IsTaskType();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTaskType_WithValueTaskType_ReturnsTrue()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");

        // Act
        var result = valueTaskType!.IsTaskType();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTaskType_WithNonTaskType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var stringType = compilation.GetTypeByMetadataName("System.String");

        // Act
        var result = stringType!.IsTaskType();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsGenericTaskType Tests

    [Fact]
    public void IsGenericTaskType_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).IsGenericTaskType());
    }

    [Fact]
    public void IsGenericTaskType_WithGenericTaskType_ReturnsTrue()
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

        // Act
        var result = methodSymbol!.ReturnType.IsGenericTaskType();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGenericTaskType_WithNonGenericTaskType_ReturnsFalse()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        // Act
        var result = taskType!.IsGenericTaskType();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetTaskResultType Tests

    [Fact]
    public void GetTaskResultType_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).GetTaskResultType());
    }

    [Fact]
    public void GetTaskResultType_WithGenericTaskType_ReturnsResultType()
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

        // Act
        var result = methodSymbol!.ReturnType.GetTaskResultType();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("String", result!.Name);
    }

    [Fact]
    public void GetTaskResultType_WithNonGenericTaskType_ReturnsNull()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        // Act
        var result = taskType!.GetTaskResultType();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IsAsync Tests

    [Fact]
    public void IsAsync_WithNullMethodSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IMethodSymbol)null!).IsAsync());
    }

    [Fact]
    public void IsAsync_WithAsyncMethod_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestClass
    {
        public async Task TestMethod() { await Task.Delay(1); }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.IsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAsync_WithTaskReturnMethod_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
namespace TestNamespace
{
    public class TestClass
    {
        public Task TestMethod() { return Task.CompletedTask; }
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.IsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAsync_WithNonAsyncMethod_ReturnsFalse()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");

        // Act
        var result = methodSymbol!.IsAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetFullNamespace Tests

    [Fact]
    public void GetFullNamespace_WithNullSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ISymbol)null!).GetFullNamespace());
    }

    [Fact]
    public void GetFullNamespace_WithNestedNamespace_ReturnsFullNamespace()
    {
        // Arrange
        var source = @"
namespace TestNamespace.SubNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.SubNamespace.TestClass");

        // Act
        var result = typeSymbol!.GetFullNamespace();

        // Assert
        Assert.Equal("TestNamespace.SubNamespace", result);
    }

    [Fact]
    public void GetFullNamespace_WithGlobalNamespace_ReturnsEmptyString()
    {
        // Arrange
        var source = @"
public class TestClass {}
";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestClass");

        // Act
        var result = typeSymbol!.GetFullNamespace();

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region GetFullTypeName Tests

    [Fact]
    public void GetFullTypeName_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).GetFullTypeName());
    }

    [Fact]
    public void GetFullTypeName_WithNamespacedType_ReturnsFullName()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = typeSymbol!.GetFullTypeName();

        // Assert
        Assert.Equal("TestNamespace.TestClass", result);
    }

    #endregion

    #region IsCancellationToken Tests

    [Fact]
    public void IsCancellationToken_WithNullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IParameterSymbol)null!).IsCancellationToken());
    }

    [Fact]
    public void IsCancellationToken_WithCancellationTokenParameter_ReturnsTrue()
    {
        // Arrange
        var source = @"
using System.Threading;
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(CancellationToken token) {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var parameter = methodSymbol!.Parameters[0];

        // Act
        var result = parameter.IsCancellationToken();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCancellationToken_WithNonCancellationTokenParameter_ReturnsFalse()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(string param) {}
    }
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        var methodSymbol = TestCompilationHelper.GetMethodSymbol(typeSymbol!, "TestMethod");
        var parameter = methodSymbol!.Parameters[0];

        // Act
        var result = parameter.IsCancellationToken();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsAccessibleFrom Tests

    [Fact]
    public void IsAccessibleFrom_WithNullTypeSymbol_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var fromSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ITypeSymbol)null!).IsAccessibleFrom(fromSymbol!));
    }

    [Fact]
    public void IsAccessibleFrom_WithNullFromSymbol_ThrowsArgumentNullException()
    {
        // Arrange
        var compilation = TestCompilationHelper.CreateCompilation("class Test {}");
        var typeSymbol = compilation.GetTypeByMetadataName("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => typeSymbol!.IsAccessibleFrom(null!));
    }

    [Fact]
    public void IsAccessibleFrom_WithPublicType_ReturnsTrue()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class PublicClass {}
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var publicType = compilation.GetTypeByMetadataName("TestNamespace.PublicClass");
        var fromType = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = publicType!.IsAccessibleFrom(fromType!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAccessibleFrom_WithSameAssemblyType_ReturnsTrue()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    internal class InternalClass {}
    public class TestClass {}
}";
        var compilation = TestCompilationHelper.CreateCompilation(source);
        var internalType = compilation.GetTypeByMetadataName("TestNamespace.InternalClass");
        var fromType = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = internalType!.IsAccessibleFrom(fromType!);

        // Assert
        Assert.True(result);
    }

    #endregion
}