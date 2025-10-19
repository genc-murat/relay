extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for return type validation in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerReturnTypeTests
    {
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}