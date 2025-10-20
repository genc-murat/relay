extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for inheritance and interface scenarios in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerInheritanceTests
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
    }
}