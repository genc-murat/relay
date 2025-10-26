extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ParameterValidator.ValidateParameterOrder functionality.
/// These tests ensure that the condition 'if (!validationContext.TypeValidator(firstParam.Type))' is properly tested.
/// </summary>
public class ParameterValidatorTests
{
    /// <summary>
    /// Tests that handlers with invalid first parameter types produce HandlerInvalidRequestParameter diagnostics.
    /// This covers the condition 'if (!validationContext.TypeValidator(firstParam.Type))' in ValidateParameterOrder.
    /// </summary>
    [Fact]
    public async Task HandlerWithInvalidFirstParameter_ProducesHandlerInvalidRequestParameterDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Handle]
    public ValueTask {|RELAY_GEN_206:HandleAsync|}(string request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with another invalid first parameter type produce HandlerInvalidRequestParameter diagnostics.
    /// This covers the condition 'if (!validationContext.TypeValidator(firstParam.Type))' in ValidateParameterOrder.
    /// </summary>
    [Fact]
    public async Task HandlerWithIntFirstParameter_ProducesHandlerInvalidRequestParameterDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Handle]
    public ValueTask {|RELAY_GEN_206:HandleAsync|}(int request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers with invalid first parameter types produce HandlerInvalidRequestParameter diagnostics.
    /// This covers the condition 'if (!validationContext.TypeValidator(firstParam.Type))' in ValidateParameterOrder.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerWithInvalidFirstParameter_ProducesHandlerInvalidRequestParameterDiagnostic()
    {
        var source = @"
using System.Threading;
            using System.Threading.Tasks;
            using Relay.Core;

public class TestHandler
{
    [Notification]
    public ValueTask {|RELAY_GEN_206:HandleAsync|}(string notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that notification handlers with another invalid first parameter type produce HandlerInvalidRequestParameter diagnostics.
    /// This covers the condition 'if (!validationContext.TypeValidator(firstParam.Type))' in ValidateParameterOrder.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerWithIntFirstParameter_ProducesHandlerInvalidRequestParameterDiagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Notification]
    public ValueTask {|RELAY_GEN_206:HandleAsync|}(int notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that handlers with valid first parameter types do NOT produce diagnostics.
    /// This tests the happy path where 'validationContext.TypeValidator(firstParam.Type)' returns true.
    /// </summary>
    [Fact]
    public async Task HandlerWithValidFirstParameter_NoDiagnostics()
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
    /// Tests that notification handlers with valid first parameter types do NOT produce diagnostics.
    /// This tests the happy path where 'validationContext.TypeValidator(firstParam.Type)' returns true.
    /// </summary>
    [Fact]
    public async Task NotificationHandlerWithValidFirstParameter_NoDiagnostics()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
            using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}