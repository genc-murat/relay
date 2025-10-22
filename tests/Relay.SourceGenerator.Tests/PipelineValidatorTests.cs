using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Validators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for PipelineValidator methods to ensure comprehensive coverage.
/// </summary>
public class PipelineValidatorTests
{
    #region Test Helper Classes

    /// <summary>
    /// Mock context for testing validation methods.
    /// </summary>
    private class TestDiagnosticReporter
    {
        public List<Diagnostic> ReportedDiagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            ReportedDiagnostics.Add(diagnostic);
        }
    }

    #endregion

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


}