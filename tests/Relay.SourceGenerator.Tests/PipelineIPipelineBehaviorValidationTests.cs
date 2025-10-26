extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for IPipelineBehavior validation in PipelineValidator.
/// These tests cover various IPipelineBehavior scenarios including the design where 
/// ValidateIPipelineBehaviorPattern has a redundant validation check.
/// </summary>
public class PipelineIPipelineBehaviorValidationTests
{
    /// <summary>
    /// Tests that valid pipeline behavior patterns (with valid delegate types) do not produce diagnostics.
    /// This tests the normal path where the method is properly identified as IPipelineBehavior.
    /// </summary>
    [Fact]
    public async Task PipelineWithValidRequestHandlerDelegateType_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public Task<string> ValidPipeline(string request, RequestHandlerDelegate<string> next, CancellationToken token)
    {
        return next(request, token);
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that 3-parameter methods with invalid delegate types are treated as generic pipelines.
    /// This shows the current behavior where (request, invalidType, CancellationToken) goes to generic pipeline validation
    /// rather than IPipelineBehavior validation, which may be the intended behavior.
    /// </summary>
    [Fact]
    public async Task ThreeParamMethodWithInvalidDelegate_TreatedAsGenericPipeline()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void GenericPipeline(string request, string invalidDelegate, CancellationToken token) { }
}";

        // This currently passes (is treated as generic pipeline), which demonstrates the design decision
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}