extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for method modifiers in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerMethodModifiersTests
    {
        /// <summary>
        /// Tests that handlers with static methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerStaticMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public static ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with private methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerPrivateMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    private ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with protected methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerProtectedMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    protected ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with internal methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerInternalMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    internal ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with abstract methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerAbstractMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public abstract class TestRequest : IRequest<string> { }

public abstract class TestHandler
{
    [Handle]
    public abstract ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken);
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with virtual methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerVirtualMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public virtual ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with sealed methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerSealedMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public sealed ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with readonly methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerReadonlyMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public readonly struct TestHandler
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
        /// Tests that handlers with unsafe methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerUnsafeMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public unsafe ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with extern methods work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerExternMethods_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using System.Runtime.InteropServices;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [DllImport(""user32.dll"")]
    public extern ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken);
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}