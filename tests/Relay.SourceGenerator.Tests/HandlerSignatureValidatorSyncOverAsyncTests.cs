extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for SyncOverAsync functionality in HandlerSignatureValidator.
/// These tests ensure that the 'if (objectType != null)' condition in ValidateAsyncSignaturePattern is properly tested.
/// </summary>
public class HandlerSignatureValidatorSyncOverAsyncTests
{
    /// <summary>
    /// Tests that handlers using .Result on Task objects do NOT produce SyncOverAsync diagnostics by default 
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task HandlerUsingResultOnTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var task = Task.FromResult(""test"");
        var result = task.Result; // This should be analyzed but not trigger diagnostic (disabled by default)
        return result;
    }
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers using .Result on ValueTask objects do NOT produce SyncOverAsync diagnostics by default
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task HandlerUsingResultOnValueTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        ValueTask<string> task = GetValueTaskAsync();
        var result = task.Result; // This should be analyzed but not trigger diagnostic (disabled by default)
        return result;
    }

    private ValueTask<string> GetValueTaskAsync() => new ValueTask<string>(""test"");
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers using .Wait() on Task objects do NOT produce SyncOverAsync diagnostics by default
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task HandlerUsingWaitOnTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var task = Task.Delay(100);
        task.Wait(); // This should be analyzed but not trigger diagnostic (disabled by default)
        return ""done"";
    }
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with missing ConfigureAwait do NOT produce MissingConfigureAwait diagnostics by default
    /// (diagnostic RELAY_GEN_104 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the await expression analysis in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task HandlerMissingConfigureAwait_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var task = Task.FromResult(""test"");
        var result = await task; // Missing .ConfigureAwait(false) should be analyzed (disabled by default)
        return result;
    }
}";

        // This test ensures that the analyzer runs without errors when processing missing ConfigureAwait
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with proper ConfigureAwait do not produce diagnostics.
    /// This tests the happy path of await expression analysis in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task HandlerWithConfigureAwait_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var task = Task.FromResult(""test"");
        var result = await task.ConfigureAwait(false);
        return result;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers using non-Task types (like regular properties named 'Result') 
    /// do not produce SyncOverAsync diagnostics and cover the objectType != null path.
    /// When the semantic model can identify that the type is not a Task type, it will go through 
    /// the type comparison logic, testing the objectType != null condition.
    /// </summary>
    [Fact]
    public async Task HandlerUsingNonTaskResult_PropertyAccessNoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestClass
{
    public string Result { get; set; } = ""value"";
}

public class TestHandler
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var obj = new TestClass();
        var result = obj.Result; // This is a property, not a Task.Result
        return result;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}