extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for the RelayAnalyzer to ensure proper validation of handler signatures and configurations.
    /// </summary>
    public class RelayAnalyzerTests
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with invalid signatures produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerInvalidSignature_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public {|RELAY_GEN_204:string|} {|RELAY_GEN_102:HandleAsync|}(TestNotification notification, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers missing notification parameter produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerMissingParameter_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_208:HandleAsync|}()
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with invalid parameter types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerInvalidParameterType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class InvalidNotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_206:HandleAsync|}(InvalidNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with extreme priority values produce performance warnings.
        /// </summary>
        [Fact]
        public async Task HandlerExtremePriorityValues_ProducesWarning()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest1 : IRequest<string> { }
public class TestRequest2 : IRequest<string> { }

public class TestHandler
{
    [Handle(Priority = -2000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleLowPriorityAsync|}(TestRequest1 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""low priority"");
    }

    [Handle(Priority = 2000)]
    public ValueTask<string> {|RELAY_GEN_102:HandleHighPriorityAsync|}(TestRequest2 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""high priority"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods missing parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task PipelineMethodMissingParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void {|RELAY_GEN_002:ExecutePipeline|}()
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that valid notification handlers do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidNotificationHandler_NoDiagnostics()
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that stream request handlers with valid signatures produce performance warnings.
        /// </summary>
        [Fact]
        public async Task ValidStreamHandler_ProducesPerformanceWarning()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestStreamRequest : IStreamRequest<string> { }

public class TestHandler
{
    [Handle]
    public IAsyncEnumerable<string> {|RELAY_GEN_102:HandleAsync|}(TestStreamRequest request, CancellationToken cancellationToken)
    {
        return AsyncEnumerable.Empty<string>();
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that stream handlers with invalid return types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task StreamHandlerInvalidReturnType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestStreamRequest : IStreamRequest<string> { }

public class TestHandler
{
    [Handle]
    public {|RELAY_GEN_203:IEnumerable<string>|} {|RELAY_GEN_102:HandleAsync|}(TestStreamRequest request, CancellationToken cancellationToken)
    {
        return new List<string>();
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with CancellationToken in wrong position produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerCancellationTokenWrongPosition_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_206:HandleAsync|}(CancellationToken cancellationToken, TestNotification notification)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that async methods with invalid return types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task AsyncMethodInvalidReturnType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public async {|RELAY_GEN_002:string|} HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(1);
        return ""invalid"";
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with complex inheritance hierarchies work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerComplexInheritance_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public interface IBaseRequest : IRequest<string> { }
public class TestRequest : IBaseRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with valid signatures do not produce diagnostics.
        /// </summary>
        [Fact]
        public async Task ValidPipelineMethod_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with invalid Order values produce diagnostics.
        /// </summary>
        [Fact]
        public async Task PipelineInvalidOrderValue_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = ""invalid"")]
    public void {|RELAY_GEN_209:ExecutePipeline|}(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that multiple handlers for different request types work correctly.
        /// </summary>
        [Fact]
        public async Task MultipleHandlersDifferentRequests_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest1 : IRequest<string> { }
public class TestRequest2 : IRequest<int> { }
public class TestRequest3 : IRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest1 request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""string"");
    }

    [Handle]
    public Task<int> HandleAsync(TestRequest2 request, CancellationToken cancellationToken)
    {
        return Task.FromResult(42);
    }

    [Handle]
    public ValueTask HandleAsync(TestRequest3 request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with missing CancellationToken produce warnings.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerMissingCancellationToken_ProducesWarning()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_207:HandleAsync|}(TestNotification notification)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with generic constraints work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerGenericConstraints_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class GenericRequest<T> : IRequest<T> where T : class { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(GenericRequest<string> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""generic"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with nested classes work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerNestedClasses_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class OuterClass
{
    public class TestRequest : IRequest<string> { }

    public class TestHandler
    {
        [Handle]
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""nested"");
        }
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with interface implementations work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerInterfaceImplementation_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public interface ITestRequest : IRequest<string> { }
public class TestRequest : ITestRequest { }

public class TestHandler : ITestRequest
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""interface"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with complex parameters work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineComplexParameters_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with Task return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerTaskReturnType_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string> { }

public class TestHandler
{
    [Handle]
    public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(""task"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with ValueTask return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerValueTaskReturnType_NoDiagnostics()
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
        return ValueTask.FromResult(""value task"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with complex inheritance hierarchies work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerComplexInheritanceMultipleInterfaces_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public interface IBaseRequest : IRequest<string> { }
public interface IDerivedRequest : IBaseRequest { }
public class TestRequest : IDerivedRequest { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""complex inheritance"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with multiple parameter types produce diagnostics.
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with multiple parameters produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerMultipleUnexpectedParameters_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_002:HandleAsync|}(TestNotification notification, string extraParam, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with CancellationToken not last produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerCancellationTokenNotLast_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_002:HandleAsync|}(TestNotification notification, CancellationToken cancellationToken, string extraParam)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with invalid attribute priority types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerInvalidAttributePriorityType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification(Priority = ""invalid"")]
    public Task {|RELAY_GEN_209:HandleAsync|}(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with invalid attribute order types produce diagnostics.
        /// </summary>
        [Fact]
        public async Task PipelineInvalidAttributeOrderType_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = ""invalid"")]
    public void {|RELAY_GEN_209:ExecutePipeline|}(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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
    public ValueTask<string> {|RELAY_GEN_102:HandleDefaultAsync|}(TestRequest request, CancellationToken cancellationToken)
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with complex parameter types work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineComplexParameterTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(Dictionary<string, object> context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }

    [Pipeline]
    public void ExecutePipelineWithList(List<string> items, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with various parameter types work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineVariousParameterTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline]
    public void ExecutePipeline(int param, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with multiple CancellationTokens produce diagnostics.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerMultipleCancellationTokens_ProducesDiagnostic()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification]
    public Task {|RELAY_GEN_002:HandleAsync|}(TestNotification notification, CancellationToken token1, CancellationToken token2)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with tuple return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerTupleReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<(string, int)> { }

public class TestHandler
{
    [Handle]
    public ValueTask<(string, int)> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((""test"", 42));
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with nullable return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerNullableReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string?> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string?> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<string?>(null);
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with dynamic return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerDynamicReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<dynamic> { }

public class TestHandler
{
    [Handle]
    public ValueTask<dynamic> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((dynamic)""test"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with record types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerRecordTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public record TestRequest : IRequest<TestResponse>;
public record TestResponse(string Value);

public class TestHandler
{
    [Handle]
    public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new TestResponse(""test""));
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with struct types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerStructTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public struct TestRequest : IRequest<TestResponse> { }
public struct TestResponse { public string Value { get; set; } }

public struct TestHandler
{
    [Handle]
    public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new TestResponse { Value = ""test"" });
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with enum types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerEnumTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public enum TestStatus { Success, Failure }
public class TestRequest : IRequest<TestStatus> { }

public class TestHandler
{
    [Handle]
    public ValueTask<TestStatus> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestStatus.Success);
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with interface return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerInterfaceReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public interface ITestResponse { string GetValue(); }
public class TestResponse : ITestResponse
{
    public string GetValue() => ""test"";
}
public class TestRequest : IRequest<ITestResponse> { }

public class TestHandler
{
    [Handle]
    public ValueTask<ITestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<ITestResponse>(new TestResponse());
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with array return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerArrayReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<string[]> { }

public class TestHandler
{
    [Handle]
    public ValueTask<string[]> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new[] { ""test1"", ""test2"" });
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with generic return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerGenericReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestRequest : IRequest<List<string>> { }

public class TestHandler
{
    [Handle]
    public ValueTask<List<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new List<string> { ""test1"", ""test2"" });
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with nested generic return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerNestedGenericReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Relay.Core;

public class TestRequest : IRequest<Dictionary<string, List<int>>> { }

public class TestHandler
{
    [Handle]
    public ValueTask<Dictionary<string, List<int>>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new Dictionary<string, List<int>>
        {
            { ""key1"", new List<int> { 1, 2, 3 } },
            { ""key2"", new List<int> { 4, 5, 6 } }
        });
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers with custom attribute combinations work correctly.
        /// </summary>
        [Fact]
        public async Task NotificationHandlerCustomAttributes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestNotification : INotification { }

public class TestHandler
{
    [Notification(Name = ""CustomNotification"", Priority = 200)]
    public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with custom attribute combinations work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineCustomAttributes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Name = ""CustomPipeline"", Order = 50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with zero order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineZeroOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = 0)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with negative order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelineNegativeOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = -50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that pipeline methods with positive order work correctly.
        /// </summary>
        [Fact]
        public async Task PipelinePositiveOrderAttribute_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestHandler
{
    [Pipeline(Order = 50)]
    public void ExecutePipeline(string context, CancellationToken cancellationToken)
    {
        // Pipeline logic
    }
}";

            await VerifyAnalyzerAsync(source);
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

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers in deeply nested classes work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerDeeplyNestedClasses_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class OuterClass
{
    public class MiddleClass
    {
        public class InnerClass
        {
            public class TestRequest : IRequest<string> { }

            public class TestHandler
            {
                [Handle]
                public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
                {
                    return ValueTask.FromResult(""deeply nested"");
                }
            }
        }
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with named tuple return types work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerNamedTupleReturnTypes_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class TestRequest : IRequest<(string Name, int Value)> { }

public class TestHandler
{
    [Handle]
    public ValueTask<(string Name, int Value)> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((""test"", 42));
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers in classes implementing multiple interfaces work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerMultipleInterfaceInheritance_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public interface IAuditable { }
public interface IVersionable { }
public class TestRequest : IRequest<string> { }

public class TestHandler : IAuditable, IVersionable
{
    [Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""multiple interfaces"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that handlers with complex generic constraints work correctly.
        /// </summary>
        [Fact]
        public async Task HandlerComplexGenericConstraints_NoDiagnostics()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;

public class GenericRequest<T> : IRequest<T> where T : class, new() { }

public class TestHandler
{
    [Handle]
    public ValueTask<string> HandleAsync(GenericRequest<string> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""complex generics"");
    }
}";

            await VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Helper method to verify analyzer diagnostics.
        /// </summary>
        private static async Task VerifyAnalyzerAsync(string source)
        {
            // Create compilation
            var compilation = CreateTestCompilation(source);
            
            // Create analyzer
            var analyzer = new RelayAnalyzer();
            
            // Create compilation with analyzers
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            
            // Get analyzer diagnostics
            var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            
            // Get expected diagnostics from markup in source
            var expectedDiagnostics = ParseExpectedDiagnostics(source);
            
            // Verify diagnostics match expectations
            VerifyDiagnostics(expectedDiagnostics, analyzerDiagnostics);
        }

        /// <summary>
        /// Creates a test compilation with the provided source.
        /// </summary>
        private static CSharpCompilation CreateTestCompilation(string source)
        {
            // Remove diagnostic markup from source before compilation
            var cleanSource = RemoveDiagnosticMarkup(source);

            // Add Relay.Core stubs
            var relayCoreStubs = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    public interface IStreamRequest<out TResponse> { }
    public interface INotification { }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandleAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NotificationAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PipelineAttribute : Attribute
    {
        public string? Name { get; set; }
        public int Order { get; set; }
    }
}";

            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(relayCoreStubs),
                CSharpSyntaxTree.ParseText(cleanSource)
            };

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        /// <summary>
        /// Removes diagnostic markup from source code.
        /// </summary>
        private static string RemoveDiagnosticMarkup(string source)
        {
            var result = source;
            var startIndex = 0;

            while ((startIndex = result.IndexOf("{|", startIndex)) != -1)
            {
                var endIndex = result.IndexOf("|}", startIndex);
                if (endIndex == -1) break;

                var beforeMarkup = result.Substring(0, startIndex);
                var afterMarkup = result.Substring(endIndex + 2);
                
                // Find the content between the markup (the actual code)
                var markupContent = result.Substring(startIndex + 2, endIndex - startIndex - 2);
                var colonIndex = markupContent.IndexOf(':');
                var content = colonIndex >= 0 ? markupContent.Substring(colonIndex + 1) : "";

                result = beforeMarkup + content + afterMarkup;
                startIndex = beforeMarkup.Length + content.Length;
            }

            return result;
        }

        /// <summary>
        /// Parses expected diagnostic markers from the source code.
        /// </summary>
        private static List<ExpectedDiagnostic> ParseExpectedDiagnostics(string source)
        {
            var expectedDiagnostics = new List<ExpectedDiagnostic>();
            var lines = source.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var startIndex = 0;

                while ((startIndex = line.IndexOf("{|", startIndex)) != -1)
                {
                    var endIndex = line.IndexOf("|}", startIndex);
                    if (endIndex == -1) break;

                    var diagnosticId = line.Substring(startIndex + 2, endIndex - startIndex - 2);
                    var colonIndex = diagnosticId.IndexOf(':');
                    
                    if (colonIndex != -1)
                    {
                        diagnosticId = diagnosticId.Substring(0, colonIndex);
                    }

                    expectedDiagnostics.Add(new ExpectedDiagnostic
                    {
                        Id = diagnosticId,
                        Line = i + 1,
                        Column = startIndex + 1
                    });

                    startIndex = endIndex + 2;
                }
            }

            return expectedDiagnostics;
        }

        /// <summary>
        /// Verifies that actual diagnostics match expected diagnostics.
        /// </summary>
        private static void VerifyDiagnostics(List<ExpectedDiagnostic> expectedDiagnostics, ImmutableArray<Diagnostic> actualDiagnostics)
        {
            var actualErrors = actualDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning).ToList();

            if (expectedDiagnostics.Count == 0 && actualErrors.Count == 0)
            {
                return; // Both empty, test passes
            }

            // Check each expected diagnostic is present
            foreach (var expected in expectedDiagnostics)
            {
                var matchingDiagnostic = actualErrors.FirstOrDefault(d => d.Id == expected.Id);
                Assert.NotNull(matchingDiagnostic);
            }

            // Check we don't have unexpected diagnostics
            foreach (var actual in actualErrors)
            {
                var expectedForThisDiagnostic = expectedDiagnostics.Any(e => e.Id == actual.Id);
                Assert.True(expectedForThisDiagnostic, $"Unexpected diagnostic '{actual.Id}': {actual.GetMessage()}");
            }
        }

        /// <summary>
        /// Represents an expected diagnostic from test markup.
        /// </summary>
        private class ExpectedDiagnostic
        {
            public string Id { get; set; } = string.Empty;
            public int Line { get; set; }
            public int Column { get; set; }
        }
    }
}