using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator.Validators;
using System.Collections.Immutable;
using System.Reflection;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for PipelineValidator methods to ensure comprehensive coverage.
/// </summary>
public class PipelineValidatorTests
{
    #region Integration Test Verification

    [Fact]
    public async Task PipelineValidator_Methods_Are_Called_From_RelayAnalyzer()
    {
        // This test verifies that PipelineValidator methods are actually called
        // by the RelayAnalyzer through integration testing

        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string context, CancellationToken token) { }

    [Pipeline(Order = 1)]
    public void AnotherPipeline(string context, CancellationToken token) { }
}";

        // This should pass without diagnostics since the methods are valid
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_Pipeline_Signatures()
    {
        // This test verifies that invalid pipeline signatures are caught
        var source = @"
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}() { }
}";

        // This should report a diagnostic for invalid pipeline signature
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
    #endregion

    #region Unit Tests for Helper Methods

    [Fact]
    public void GetScopeName_Returns_Correct_Names()
    {
        // Act & Assert
        var result0 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 0 });
        Assert.Equal("All", result0);

        var result1 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 1 });
        Assert.Equal("Requests", result1);

        var result2 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 2 });
        Assert.Equal("Streams", result2);

        var result3 = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 3 });
        Assert.Equal("Notifications", result3);

        var resultUnknown = (string)typeof(PipelineValidator).GetMethod("GetScopeName", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { 99 });
        Assert.Equal("Unknown", resultUnknown);
    }

    #endregion

    #region Enhanced Integration Tests

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_IPipelineBehavior_Return_Type()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Reports_Invalid_CancellationToken_In_IPipelineBehavior()
    {
        var source = @"
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public Task<string> {|RELAY_GEN_002:InvalidPipeline|}(string request, RequestHandlerDelegate<string> next, string invalidToken)
    {
        return Task.FromResult(string.Empty);
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PipelineValidator_Validates_Stream_Pipeline_Signature()
    {
        var source = @"
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public IAsyncEnumerable<string> ValidStreamPipeline(string request, StreamHandlerDelegate<string> next, CancellationToken token)
    {
        return next();
    }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Helper Methods

    private static CSharpCompilation CreateTestCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
            // Add reference to basic .NET types
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    #endregion

    #region Test Helper Classes

    public class PipelineAttribute : Attribute
    {
        public int Order { get; set; }
        public int Scope { get; set; }
    }

    #endregion
}