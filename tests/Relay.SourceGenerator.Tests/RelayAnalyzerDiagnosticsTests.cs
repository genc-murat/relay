extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for diagnostic scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerDiagnosticsTests
    {
        /// <summary>
        /// Tests that handlers with override methods produce duplicate handler diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerOverrideMethods_ProducesDuplicateDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public abstract class BaseHandler
{
    [Handle]
    public abstract ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken);
}

public class TestHandler : BaseHandler
{
    [Handle]
    public override ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with new methods produce duplicate handler diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerNewMethods_ProducesDuplicateDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class BaseHandler
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""base"");
    }
}

public class TestHandler : BaseHandler
{
    [Handle]
    public new ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""derived"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with partial methods produce duplicate handler diagnostics.
        /// </summary>
        [Fact]
        public async Task HandlerPartialMethods_ProducesDuplicateDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public partial class TestHandler
{
    [Handle]
    public partial ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken);
}

public partial class TestHandler
{
    public partial ValueTask<string> {|RELAY_GEN_003:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that async void handlers produce diagnostics.
        /// </summary>
        [Fact]
        public async Task AsyncVoidHandler_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async {|RELAY_GEN_202:void|} {|RELAY_GEN_002:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that async notification handlers with void return produce diagnostics.
        /// </summary>
        [Fact]
        public async Task AsyncVoidNotificationHandler_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async {|RELAY_GEN_204:void|} {|RELAY_GEN_002:HandleAsync|}(TestNotification notification, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
    }
}";

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}