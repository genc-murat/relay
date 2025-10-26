extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for attribute validation in the RelayAnalyzer.
/// </summary>
public class RelayAnalyzerAttributeTests
{
    /// <summary>
    /// Tests that invalid priority values produce diagnostics.
    /// </summary>
    [Fact]
    public async Task InvalidPriorityValue_ProducesDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }
public class TestNotification : INotification { }

public class TestHandler
{
    [Handle(Priority = ""invalid"")]
    public ValueTask<string> {|RELAY_GEN_209:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }

    [Notification(Priority = 123.45)]
    public Task {|RELAY_GEN_209:HandleNotificationAsync|}(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that named handler conflicts produce diagnostics.
    /// </summary>
    [Fact]
    public async Task NamedHandlerConflict_ProducesDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler1
{
    [Handle(Name = ""MyHandler"")]
    public ValueTask<string> {|RELAY_GEN_005:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test1"");
    }
}

public class TestHandler2
{
    [Handle(Name = ""MyHandler"")]
    public ValueTask<string> {|RELAY_GEN_005:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test2"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that mixed named and unnamed handlers produce configuration conflict diagnostics.
    /// </summary>
    [Fact]
    public async Task MixedNamedUnnamedHandlers_ProducesDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler1
{
    [Handle]
    public ValueTask<string> {|RELAY_GEN_211:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test1"");
    }
}

public class TestHandler2
{
    [Handle(Name = ""NamedHandler"")]
    public ValueTask<string> HandleNamedAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test2"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that non-async methods with invalid return types produce performance warnings.
    /// </summary>
    [Fact]
    public async Task NonAsyncMethodInvalidReturnType_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public {|RELAY_GEN_102:string|} HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ""invalid"";
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with very high priority values produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerVeryHighPriority_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = 5000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""high priority"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with very low priority values produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerVeryLowPriority_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = -5000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""low priority"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with common naming conflicts produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerCommonNamingConflicts_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest1 : IRequest<string> { }
public class TestRequest2 : IRequest<string> { }

public class TestHandler
{
    [Handle(Name = ""default"")]
    public ValueTask<string> {|RELAY_GEN_102:HandleDefaultAsync|}(TestRequest1 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""default"");
    }

    [Handle(Name = ""main"")]
    public ValueTask<string> {|RELAY_GEN_102:HandleMainAsync|}(TestRequest2 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""main"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with common naming conflicts produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerCommonNamingConflictsCompilation_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler1
{
    [Handle(Name = ""default"")]
    public ValueTask<string> {|RELAY_GEN_102:HandleDefaultAsync|}{|RELAY_GEN_211:HandleDefaultAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""default"");
    }
}

public class TestHandler2
{
    [Handle(Name = ""main"")]
    public ValueTask<string> {|RELAY_GEN_102:HandleMainAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""main"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with invalid attribute priority types produce diagnostics.
    /// </summary>
    [Fact]
    public async Task HandlerInvalidAttributePriorityType_ProducesDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = ""invalid"")]
    public ValueTask<string> {|RELAY_GEN_209:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with multiple Relay attributes produce diagnostics.
    /// </summary>
    [Fact]
    public async Task HandlerMultipleRelayAttributes_ProducesDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }
public class TestNotification : INotification { }

public class TestHandler
{
    [Handle]
    [Notification]
    public ValueTask<string> {|RELAY_GEN_206:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that methods with multiple Relay attributes work correctly.
    /// </summary>
    [Fact]
    public async Task MethodMultipleRelayAttributes_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    [Pipeline]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with empty names work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerEmptyNameAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Name = """")]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with whitespace names work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerWhitespaceNameAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Name = ""   "")]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with null names work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerNullNameAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Name = null)]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with zero priority work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerZeroPriorityAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = 0)]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with negative priority work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerNegativePriorityAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = -100)]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with positive priority work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerPositivePriorityAttribute_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = 100)]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with custom attribute combinations work correctly.
    /// </summary>
    [Fact]
    public async Task HandlerCustomAttributes_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Name = ""CustomHandler"", Priority = 100)]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with extreme negative priorities produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerExtremeNegativePriority_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = -5000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""extreme negative priority"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with extreme positive priorities produce warnings.
    /// </summary>
    [Fact]
    public async Task HandlerExtremePositivePriority_ProducesWarning()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = 5000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleAsync|}(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""extreme positive priority"");
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}