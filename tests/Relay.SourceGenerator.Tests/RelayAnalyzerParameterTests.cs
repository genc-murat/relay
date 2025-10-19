extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for parameter validation in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerParameterTests
    {
        /// <summary>
        /// Tests that handlers with incorrect parameter order produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerIncorrectParameterOrder_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken cancellationToken, string extraParam)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with unexpected parameter types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerUnexpectedParameterTypes_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken cancellationToken, string unexpectedParam)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with CancellationToken in wrong position produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerCancellationTokenWrongPosition_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_206:HandleAsync|}(CancellationToken cancellationToken, TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with multiple unexpected parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerMultipleUnexpectedParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, string extraParam, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with parameters before CancellationToken produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerCancellationTokenNotLast_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken cancellationToken, string extraParam)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with multiple CancellationTokens produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerMultipleCancellationTokens_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken token1, CancellationToken token2)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with ref/out parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerRefOutParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, ref string output, CancellationToken cancellationToken)
    {
        output = ""test"";
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with params parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerParamsParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, params string[] args, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with optional parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerOptionalParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}(TestRequest request, string optional = ""default"", CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with generic type parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerGenericMethodParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_002:HandleAsync|}<T>(TestRequest request, T genericParam, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}