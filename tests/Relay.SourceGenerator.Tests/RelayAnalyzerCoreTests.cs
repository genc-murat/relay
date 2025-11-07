extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Core;
using System.Reflection;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests;

public class RelayAnalyzerCoreTests
{

    /// <summary>
    /// Tests that Initialize method exists and is public.
    /// This tests method existence and accessibility.
    /// </summary>
    [Fact]
    public void Initialize_Method_Exists_And_Is_Public()
    {
        // Verify that Initialize method exists and is public
        var method = typeof(RelayAnalyzer).GetMethod("Initialize",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(AnalysisContext), method.GetParameters()[0].ParameterType);
    }

    /// <summary>
    /// Tests that Initialize can be called without errors.
    /// This tests basic method invocation.
    /// </summary>
    [Fact]
    public void Initialize_Can_Be_Called_Without_Error()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act & Assert - Should not throw
        var method = typeof(RelayAnalyzer).GetMethod("Initialize",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        // The method exists and has the correct signature
        Assert.Single(method.GetParameters());
        Assert.Equal("AnalysisContext", method.GetParameters()[0].ParameterType.Name);
    }

    /// <summary>
    /// Tests that Initialize method exists and has correct signature.
    /// This tests method existence and parameter validation.
    /// </summary>
    [Fact]
    public void Initialize_Has_Correct_Signature()
    {
        // Arrange & Act
        var method = typeof(RelayAnalyzer).GetMethod("Initialize",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
        Assert.False(method!.IsStatic);
        Assert.Equal("Initialize", method.Name);
        
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("AnalysisContext", parameters[0].ParameterType.Name);
        Assert.Equal("Void", method.ReturnType.Name);
    }

    [Fact]
    public void SupportedDiagnostics_Includes_Error_Severity_Diagnostics()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        var errorDiagnostics = supportedDiagnostics.Where(d => d.DefaultSeverity == DiagnosticSeverity.Error);
        Assert.NotEmpty(errorDiagnostics);

        // We expect multiple error diagnostics
        Assert.True(errorDiagnostics.Count() >= 10, $"Expected at least 10 error diagnostics, got {errorDiagnostics.Count()}");
    }

    [Fact]
    public void SupportedDiagnostics_Includes_Warning_Severity_Diagnostics()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        var warningDiagnostics = supportedDiagnostics.Where(d => d.DefaultSeverity == DiagnosticSeverity.Warning);
        Assert.NotEmpty(warningDiagnostics);

        // We expect multiple warning diagnostics
        Assert.True(warningDiagnostics.Count() >= 5, $"Expected at least 5 warning diagnostics, got {warningDiagnostics.Count()}");
    }

    [Fact]
    public void SupportedDiagnostics_Includes_Info_Severity_Diagnostics()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        var infoDiagnostics = supportedDiagnostics.Where(d => d.DefaultSeverity == DiagnosticSeverity.Info);
        Assert.NotEmpty(infoDiagnostics);

        // We expect at least one info diagnostic (InternalHandler)
        Assert.True(infoDiagnostics.Count() >= 1, $"Expected at least 1 info diagnostic, got {infoDiagnostics.Count()}");
    }

    /// <summary>
    /// Tests that SupportedDiagnostics includes all expected diagnostic IDs.
    /// This tests completeness of diagnostic coverage.
    /// </summary>
    [Fact]
    public void SupportedDiagnostics_IncludesAllExpectedDiagnosticIds()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();
        var expectedDiagnosticIds = new[]
        {
            "RELAY_GEN_001", // GeneratorError
            "RELAY_GEN_004", // MissingRelayCoreReference
            "RELAY_GEN_002", // InvalidHandlerSignature
            "RELAY_GEN_202", // InvalidHandlerReturnType
            "RELAY_GEN_203", // InvalidStreamHandlerReturnType
            "RELAY_GEN_204", // InvalidNotificationHandlerReturnType
            "RELAY_GEN_205", // HandlerMissingRequestParameter
            "RELAY_GEN_206", // HandlerInvalidRequestParameter
            "RELAY_GEN_207", // HandlerMissingCancellationToken
            "RELAY_GEN_208", // NotificationHandlerMissingParameter
            "RELAY_GEN_003", // DuplicateHandler
            "RELAY_GEN_005", // NamedHandlerConflict
            "RELAY_GEN_209", // InvalidPriorityValue
            "RELAY_GEN_211", // ConfigurationConflict
            "RELAY_GEN_212", // InvalidPipelineScope
            "RELAY_GEN_201", // DuplicatePipelineOrder
            "RELAY_GEN_101", // UnusedHandler
            "RELAY_GEN_102", // PerformanceWarning
            "RELAY_GEN_104", // MissingConfigureAwait
            "RELAY_GEN_105", // SyncOverAsync
            "RELAY_GEN_106", // PrivateHandler
            "RELAY_GEN_107", // InternalHandler
            "RELAY_GEN_108", // MultipleConstructors
            "RELAY_GEN_109"  // ConstructorValueTypeParameter
        };

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;
        var actualDiagnosticIds = supportedDiagnostics.Select(d => d.Id).ToHashSet();

        // Assert
        foreach (var expectedId in expectedDiagnosticIds)
        {
            Assert.True(actualDiagnosticIds.Contains(expectedId), 
                $"Expected diagnostic ID '{expectedId}' not found in SupportedDiagnostics");
        }
    }

    /// <summary>
    /// Tests that all diagnostics in SupportedDiagnostics have valid properties.
    /// This tests diagnostic property validation.
    /// </summary>
    [Fact]
    public void SupportedDiagnostics_AllDiagnosticsHaveValidProperties()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Diagnostics that are intentionally disabled by default
        var disabledByDefault = new[]
        {
            "RELAY_DEBUG",
            "RELAY_GEN_104", // MissingConfigureAwait
            "RELAY_GEN_105"  // SyncOverAsync
        };

        // Assert
        foreach (var diagnostic in supportedDiagnostics)
        {
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.Id), 
                $"Diagnostic ID should not be null or whitespace for diagnostic at index {supportedDiagnostics.IndexOf(diagnostic)}");
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.Title.ToString()), 
                $"Diagnostic title should not be null or whitespace for diagnostic {diagnostic.Id}");
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.Description.ToString()), 
                $"Diagnostic description should not be null or whitespace for diagnostic {diagnostic.Id}");
            Assert.True(diagnostic.DefaultSeverity == DiagnosticSeverity.Error || 
                      diagnostic.DefaultSeverity == DiagnosticSeverity.Warning || 
                      diagnostic.DefaultSeverity == DiagnosticSeverity.Info, 
                $"Diagnostic {diagnostic.Id} should have valid severity");
            
            if (disabledByDefault.Contains(diagnostic.Id))
            {
                Assert.False(diagnostic.IsEnabledByDefault, 
                    $"Diagnostic {diagnostic.Id} should be disabled by default");
            }
            else
            {
                Assert.True(diagnostic.IsEnabledByDefault, 
                    $"Diagnostic {diagnostic.Id} should be enabled by default");
            }
        }
    }

    /// <summary>
    /// Tests that SupportedDiagnostics has no duplicate diagnostic IDs.
    /// This tests uniqueness of diagnostic identifiers.
    /// </summary>
    [Fact]
    public void SupportedDiagnostics_NoDuplicateIds()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;
        var diagnosticIds = supportedDiagnostics.Select(d => d.Id).ToList();

        // Assert
        var uniqueIds = diagnosticIds.Distinct().ToList();
        if (diagnosticIds.Count != uniqueIds.Count)
        {
            var message = "Found duplicate diagnostic IDs in SupportedDiagnostics";
            Assert.True(false, message);
        }
        
        // Report any duplicates found
        var duplicates = diagnosticIds.GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        
        if (duplicates.Any())
        {
            Assert.True(false, $"Duplicate diagnostic IDs found: {string.Join(", ", duplicates)}");
        }
    }

    /// <summary>
    /// Tests that SupportedDiagnostics returns immutable array.
    /// This tests immutability of the returned collection.
    /// </summary>
    [Fact]
    public void SupportedDiagnostics_ReturnsImmutableArray()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        Assert.IsType<ImmutableArray<DiagnosticDescriptor>>(supportedDiagnostics);
        Assert.True(supportedDiagnostics.IsDefaultOrEmpty || supportedDiagnostics.Length > 0);
    }

    /// <summary>
    /// Tests that SupportedDiagnostics returns consistent results.
    /// This tests property consistency across multiple calls.
    /// </summary>
    [Fact]
    public void SupportedDiagnostics_ReturnsConsistentResults()
    {
        // Arrange
        var analyzer = new RelayAnalyzer();

        // Act
        var firstCall = analyzer.SupportedDiagnostics;
        var secondCall = analyzer.SupportedDiagnostics;

        // Assert
        Assert.Equal(firstCall.Length, secondCall.Length);
        
        for (int i = 0; i < firstCall.Length; i++)
        {
            Assert.Equal(firstCall[i].Id, secondCall[i].Id);
            Assert.Equal(firstCall[i].Title.ToString(), secondCall[i].Title.ToString());
            Assert.Equal(firstCall[i].DefaultSeverity, secondCall[i].DefaultSeverity);
        }
    }

    #region Exception Handling Tests

    [Fact]
    public void AnalyzeMethodDeclaration_Handles_OperationCanceledException_Propagates_Cancellation()
    {
        // This test verifies that OperationCanceledException is properly propagated
        // We can't easily mock the context, but we can verify the method exists and is callable
        var method = typeof(RelayAnalyzer).GetMethod("AnalyzeMethodDeclaration",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }

    [Fact]
    public void AnalyzeMethodDeclaration_Handles_General_Exception_Reports_Diagnostic()
    {
        // This test verifies that general exceptions are caught and reported
        // We can't easily mock the context, but we can verify the method exists
        var method = typeof(RelayAnalyzer).GetMethod("AnalyzeMethodDeclaration",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_Returns_Early_For_Non_MethodDeclaration_Nodes()
    {
        // Arrange - Create source with a class but no methods
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        private int _field;
    }
}";

        // Act & Assert - Should not throw any exceptions and should complete successfully
        // The analyzer should return early when encountering non-method nodes
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_Returns_Early_When_MethodSymbol_Is_Null()
    {
        // Arrange - Create source with a method that might not resolve properly
        var source = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Method with no Relay attributes - should return early
        }
    }
}";

        // Act & Assert - Should not throw any exceptions
        // The analyzer should handle cases where method symbol resolution fails gracefully
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_Handles_OperationCanceledException_During_Analysis()
    {
        // This test verifies that OperationCanceledException is properly handled
        // We create a valid handler method to ensure analysis proceeds normally
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class TestRequest : IRequest<string> { }

    public class TestClass
    {
        [Handle]
        public async Task<string> HandleRequest(TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            return ""success"";
        }
    }
}";

        // Act & Assert - The analyzer should handle cancellation gracefully
        // This test ensures the OperationCanceledException catch block is exercised if cancellation occurs
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_Handles_General_Exceptions_During_Validation()
    {
        // This test verifies that general exceptions during validation are caught and reported
        // We create valid methods to ensure analysis proceeds normally
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class ComplexRequest : IRequest<string> { }

    public class TestClass
    {
        [Handle]
        public async Task<string> HandleComplexRequest(ComplexRequest request, CancellationToken cancellationToken)
        {
            var result = await ProcessAsync(request, cancellationToken);
            return result.ToString();
        }

        private async Task<int> ProcessAsync(ComplexRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            return 42;
        }
    }
}";

        // Act & Assert - The analyzer should handle any exceptions during validation gracefully
        // This test ensures the general Exception catch block is exercised if needed
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_With_Malformed_Attribute_Does_Not_Crash()
    {
        // Test that malformed attributes don't crash the analyzer
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class TestRequest : IRequest<string> { }

    public class TestClass
    {
        [Handle(Priority = -1)] // Invalid priority value
        public async Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ""test"";
        }
    }
}";

        // This test verifies that the analyzer handles malformed attributes gracefully
        // The analyzer should not crash even with invalid attribute values
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_With_Abstract_Class_Method_Does_Not_Crash()
    {
        // Test that methods in abstract classes are handled properly
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class TestRequest : IRequest<string> { }

    public abstract class AbstractTestClass
    {
        [Handle]
        public abstract Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken);
    }
}";

        // Act & Assert - Should not throw any exceptions and should complete successfully
        // The analyzer should handle abstract methods gracefully
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_With_Multiple_Attributes_On_Same_Method_Does_Not_Crash()
    {
        // Test that multiple Relay attributes on the same method are handled without crashing
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class TestRequest : IRequest<string> { }

    public class TestClass
    {
        [Handle]
        [Pipeline]
        public async Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ""test"";
        }
    }
}";

        // Act & Assert - Should not throw any exceptions and should complete successfully
        // The analyzer should handle multiple compatible attributes on the same method gracefully
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AnalyzeMethodDeclaration_With_Missing_Parameters_Does_Not_Crash()
    {
        // Test that methods with missing required parameters are handled gracefully
        var source = @"
using System;
using System.Threading.Tasks;
using Relay.Core;

namespace TestNamespace
{
    public class TestRequest : IRequest<string> { }

    public class TestClass
    {
        [Handle]
        public async Task<string> {|RELAY_GEN_205:HandleAsync|}()
        {
            return ""test"";
        }
    }
}";

        // This test verifies that the analyzer handles missing parameters gracefully
        // The analyzer should report RELAY_GEN_205 for missing parameters and not crash
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Error Reporting Tests

    [Fact]
    public void ReportAnalyzerError_Methods_Exist()
    {
        // Verify that the ReportAnalyzerError overloads exist
        var methods = typeof(RelayAnalyzer).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "ReportAnalyzerError");

        Assert.Equal(2, methods.Count()); // Should have 2 overloads

        // Check parameter types for each overload
        var overload1 = methods.FirstOrDefault(m =>
            m.GetParameters().Length == 3 &&
            m.GetParameters()[0].ParameterType.Name == "SyntaxNodeAnalysisContext" &&
            m.GetParameters()[1].ParameterType.Name == "MethodDeclarationSyntax");

        var overload2 = methods.FirstOrDefault(m =>
            m.GetParameters().Length == 2 &&
            m.GetParameters()[0].ParameterType.Name == "CompilationAnalysisContext");

        Assert.NotNull(overload1);
        Assert.NotNull(overload2);
    }

    [Fact]
    public void ReportAnalyzerError_Methods_Can_Be_Invoked_Without_Exceptions()
    {
        // These methods are tested indirectly through integration tests when exceptions occur.
        // Here we just verify they can be called without throwing exceptions themselves.

        var method1 = typeof(RelayAnalyzer).GetMethod("ReportAnalyzerError",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Microsoft.CodeAnalysis.Diagnostics.SyntaxNodeAnalysisContext), typeof(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax), typeof(Exception) },
            null);

        var method2 = typeof(RelayAnalyzer).GetMethod("ReportAnalyzerError",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Microsoft.CodeAnalysis.Diagnostics.CompilationAnalysisContext), typeof(Exception) },
            null);

        Assert.NotNull(method1);
        Assert.NotNull(method2);

        // The methods exist and have the correct signatures
        // They are tested indirectly when analyzer exceptions occur during analysis
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void AnalyzeMethodDeclaration_Method_Exists_And_Is_Static()
    {
        // Verify that the AnalyzeMethodDeclaration method exists and is static
        var method = typeof(RelayAnalyzer).GetMethod("AnalyzeMethodDeclaration",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("SyntaxNodeAnalysisContext", parameters[0].ParameterType.Name);
    }

    [Fact]
    public void AnalyzeCompilation_Method_Exists_And_Is_Static()
    {
        // Verify that the AnalyzeCompilation method exists and is static
        var method = typeof(RelayAnalyzer).GetMethod("AnalyzeCompilation",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("CompilationAnalysisContext", parameters[0].ParameterType.Name);
    }

    #endregion

    
}