extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for advanced handler scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerAdvancedTests
    {
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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