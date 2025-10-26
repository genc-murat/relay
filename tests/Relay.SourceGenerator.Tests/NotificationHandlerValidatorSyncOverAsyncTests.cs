extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for SyncOverAsync functionality in NotificationHandlerValidator.
/// These tests ensure that the 'if (objectType != null)' condition in ValidateAsyncSignaturePattern is properly tested.
/// </summary>
public class NotificationHandlerValidatorSyncOverAsyncTests
{
    /// <summary>
    /// Tests that notification handlers using .Result on Task objects do NOT produce SyncOverAsync diagnostics by default 
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerUsingResultOnTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        var task = Task.Delay(100);
        task.Result; // This should be analyzed but not trigger diagnostic (disabled by default)
        return;
    }
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers using .Result on ValueTask objects do NOT produce SyncOverAsync diagnostics by default
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerUsingResultOnValueTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        ValueTask task = GetValueTaskAsync();
        task.Result; // This should be analyzed but not trigger diagnostic (disabled by default)
        return;
    }

    private ValueTask GetValueTaskAsync() => new ValueTask(Task.CompletedTask);
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers using .Wait() on Task objects do NOT produce SyncOverAsync diagnostics by default
    /// (diagnostic RELAY_GEN_105 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the objectType != null condition in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerUsingWaitOnTask_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        var task = Task.Delay(100);
        task.Wait(); // This should be analyzed but not trigger diagnostic (disabled by default)
        return;
    }
}";

        // This test ensures that the analyzer runs without errors when processing sync-over-async code
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers with missing ConfigureAwait do NOT produce MissingConfigureAwait diagnostics by default
    /// (diagnostic RELAY_GEN_104 is disabled by default), but verifies the analyzer runs without errors.
    /// This covers the await expression analysis in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerMissingConfigureAwait_AnalyzerDoesNotThrow()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        var task = Task.Delay(100);
        await task; // Missing .ConfigureAwait(false) should be analyzed (disabled by default)
        return;
    }
}";

        // This test ensures that the analyzer runs without errors when processing missing ConfigureAwait
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers with proper ConfigureAwait do not produce diagnostics.
    /// This tests the happy path of await expression analysis in ValidateAsyncSignaturePattern.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerWithConfigureAwait_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        var task = Task.Delay(100);
        await task.ConfigureAwait(false);
        return;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers using non-Task types (like regular properties named 'Result') 
    /// do not produce SyncOverAsync diagnostics and cover the objectType != null path.
    /// When the semantic model can identify that the type is not a Task type, it will go through 
    /// the type comparison logic, testing the objectType != null condition.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerUsingNonTaskResult_PropertyAccessNoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestClass
{
    public string Result { get; set; } = ""value"";
}

public class TestHandler
{
    [Notification]
    public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        var obj = new TestClass();
        var result = obj.Result; // This is a property, not a Task.Result
        return;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}