extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for generic scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerGenericsTests
    {
        /// <summary>
        /// Tests that handlers with generic constraints work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerGenericConstraints_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class GenericRequest<T> : IRequest<T> where T : class { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(GenericRequest<string> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""generic"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with complex generic constraints work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerComplexGenericConstraints_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class GenericRequest<T> : IRequest<T> where T : class, new() { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(GenericRequest<string> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""complex generics"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with named tuple return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerNamedTupleReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<(string Name, int Value)> { }

public class TestHandler
{
    [Handle]
    public ValueTask<(string Name, int Value)> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((""test"", 42));
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}