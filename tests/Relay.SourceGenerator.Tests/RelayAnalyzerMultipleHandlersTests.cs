extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for multiple handlers scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerMultipleHandlersTests
    {
        /// <summary>
        /// Tests that multiple handlers for different request types work correctly.
        /// </summary>
        [Fact]
        public async Task MultipleHandlersDifferentRequests_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest1 : IRequest<string> { }
public class TestRequest2 : IRequest<int> { }
public class TestRequest3 : IRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest1 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""string"");
    }

    [Handle]
    public Task<int> HandleAsync(TestRequest2 request, CancellationToken cancellationToken)
    {
        return Task.FromResult(42);
    }

    [Handle]
    public ValueTask HandleAsync(TestRequest3 request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}