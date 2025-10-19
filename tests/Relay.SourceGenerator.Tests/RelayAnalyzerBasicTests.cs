extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for basic handler validation in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerBasicTests
    {
        /// <summary>
        /// Tests that valid handler methods do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidHandler_NoDiagnostics()
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
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers missing request parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerMissingRequestParameter_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_205:HandleAsync|}()
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with invalid return types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerInvalidReturnType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public {|RELAY_GEN_202:int|} {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return 42;
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that duplicate handlers produce diagnostics.
        /// </summary>
        [Fact]
        public async Task DuplicateHandlers_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler1
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test1"");
    }
}

public class TestHandler2
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test2"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers missing CancellationToken produce warnings.
        /// </summary>
        [Fact]
        public async Task HandlerMissingCancellationToken_ProducesWarning()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with invalid request parameter types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerInvalidRequestParameterType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class InvalidRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_206:HandleAsync|}(InvalidRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that valid void handlers do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidVoidHandler_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestVoidRequest : IRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that void handlers with invalid return types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task VoidHandlerInvalidReturnType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestVoidRequest : IRequest { }

public class TestHandler
{
    [Handle]
    public {|RELAY_GEN_202:string|} {|RELAY_GEN_102:HandleAsync|}(TestVoidRequest request, CancellationToken cancellationToken)
    {
        return ""invalid"";
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}