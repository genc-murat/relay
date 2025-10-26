namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for AttributeValidator methods to ensure comprehensive coverage.
/// </summary>
public class AttributeValidatorTests
{
    #region Integration Tests for AttributeValidator

    [Fact]
    public async Task AttributeValidator_Valid_Handle_Priority_Int_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestClass
{
    [Handle(Priority = 5)]
    public ValueTask<string> ValidHandle(TestRequest request, CancellationToken cancellationToken) { return ValueTask.FromResult(string.Empty); }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Handle_Priority_String_Reports_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestClass
{
    [Handle(Priority = ""invalid"")]
    public ValueTask<string> {|RELAY_GEN_209:InvalidHandle|}(TestRequest request, CancellationToken cancellationToken) { return ValueTask.FromResult(string.Empty); }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Handle_No_Priority_Parameter_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestClass
{
    [Handle]
    public ValueTask<string> ValidHandle(TestRequest request, CancellationToken cancellationToken) { return ValueTask.FromResult(string.Empty); }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Valid_Notification_Priority_Int_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestClass
{
    [Notification(Priority = 10)]
    public Task ValidNotification(TestNotification notification, CancellationToken cancellationToken) { return Task.CompletedTask; }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Notification_Priority_Null_Reports_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestClass
{
    [Notification(Priority = null)]
    public Task {|RELAY_GEN_209:InvalidNotification|}(TestNotification notification, CancellationToken cancellationToken) { return Task.CompletedTask; }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Notification_No_Priority_Parameter_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestClass
{
    [Notification]
    public Task ValidNotification(TestNotification notification, CancellationToken cancellationToken) { return Task.CompletedTask; }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Valid_Pipeline_Order_Int_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Order = 1)]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Pipeline_Order_Bool_Reports_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Order = true)]
    public void {|RELAY_GEN_209:InvalidPipeline|}(string context, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Valid_Pipeline_Scope_In_Range_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Scope = 2)]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Pipeline_Scope_Out_Of_Range_Reports_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [{|RELAY_GEN_212:Pipeline(Scope = 5)|}]
    public void InvalidPipeline(string context, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Pipeline_Scope_Negative_Reports_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [{|RELAY_GEN_212:Pipeline(Scope = -1)|}]
    public void InvalidPipeline(string context, CancellationToken token) { }
}";

        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Invalid_Pipeline_Scope_Type_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Scope = ""invalid"")]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics (graceful handling of non-int)
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Valid_Pipeline_Scope_Zero_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Scope = 0)]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics (boundary value 0)
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Valid_Pipeline_Scope_Three_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline(Scope = 3)]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics (boundary value 3)
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributeValidator_Pipeline_No_Parameters_No_Diagnostic()
    {
        var source = @"
using System.Threading;
using Relay.Core;

public class TestClass
{
    [Pipeline]
    public void ValidPipeline(string context, CancellationToken token) { }
}";

        // Should pass without diagnostics
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    #endregion
}