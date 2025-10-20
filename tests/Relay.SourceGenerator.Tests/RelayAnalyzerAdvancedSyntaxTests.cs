extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for advanced C# syntax scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerAdvancedSyntaxTests
    {
        /// <summary>
        /// Tests that handlers with expression-bodied methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerExpressionBodiedMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(""test"");
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with local functions work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerLocalFunctions_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return LocalHandler();

        ValueTask<string> LocalHandler() => ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with anonymous methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerAnonymousMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        Func<ValueTask<string>> handler = () => ValueTask.FromResult(""test"");
        return handler();
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with lambda expressions work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerLambdaExpressions_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        Func<string, ValueTask<string>> handler = s => ValueTask.FromResult(s);
        return handler(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with async lambda expressions work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerAsyncLambdaExpressions_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        Func<Task<string>> handler = async () => await Task.FromResult(""test"");
        return new ValueTask<string>(handler());
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers in file-scoped namespaces work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerFileScopedNamespaces_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

namespace MyNamespace;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""file scoped"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}