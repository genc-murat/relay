extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Relay.SourceGenerator.CodeFixes;
using Relay.SourceGenerator.Core;
using System.Threading.Tasks;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for RelayCodeFixProvider to ensure proper code fixes for handler signatures.
    /// </summary>
    public class RelayCodeFixProviderTests
    {
        private readonly RelayCodeFixProvider _codeFixProvider;

        public RelayCodeFixProviderTests()
        {
            _codeFixProvider = new RelayCodeFixProvider();
        }

        /// <summary>
        /// Tests that FixableDiagnosticIds returns correct diagnostic ID.
        /// </summary>
        [Fact]
        public void FixableDiagnosticIds_ReturnsCorrectId()
        {
            var fixableIds = _codeFixProvider.FixableDiagnosticIds;
            Assert.Single(fixableIds);
            Assert.Equal("RELAY_GEN_207", fixableIds[0]);
        }

        /// <summary>
        /// Tests that GetFixAllProvider returns BatchFixer.
        /// </summary>
        [Fact]
        public void GetFixAllProvider_ReturnsBatchFixer()
        {
            var fixAllProvider = _codeFixProvider.GetFixAllProvider();
            Assert.NotNull(fixAllProvider);
        }

        /// <summary>
        /// Tests that code fix adds CancellationToken parameter to a handler method.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    public interface INotification { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class NotificationAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    public interface INotification { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class NotificationAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix works with Task return type.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_TaskReturnType_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public Task<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return Task.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix adds System.Threading using when not present.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_WithoutSystemThreading_FixesHandlerMethod()
        {
            var source = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public Task<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return Task.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public Task<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken)
    {
        return Task.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix works with nested classes.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_NestedClass_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    public class OuterClass
    {
        public class TestRequest : Relay.Core.IRequest<string> { }

        public class TestHandler
        {
            [Relay.Core.Handle]
            public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
            {
                return ValueTask.FromResult(""test"");
            }
        }
    }
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }

    public class OuterClass
    {
        public class TestRequest : Relay.Core.IRequest<string> { }

        public class TestHandler
        {
            [Relay.Core.Handle]
            public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(""test"");
            }
        }
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix works with expression-bodied methods.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_ExpressionBody_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request) => ValueTask.FromResult(""test"");
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(""test"");
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix works with file-scoped namespaces.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_FileScopedNamespace_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core;

public interface IRequest { }
public interface IRequest<out TResponse> { }

[System.AttributeUsage(System.AttributeTargets.Method)]
public sealed class HandleAttribute : System.Attribute
{
    public string? Name { get; set; }
    public int Priority { get; set; }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core;

public interface IRequest { }
public interface IRequest<out TResponse> { }

[System.AttributeUsage(System.AttributeTargets.Method)]
public sealed class HandleAttribute : System.Attribute
{
    public string? Name { get; set; }
    public int Priority { get; set; }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix preserves attributes and modifiers.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_PreservesAttributesAndModifiers_FixesHandlerMethod()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    [System.Obsolete]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    [System.Obsolete]
    public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that code fix works when syntax root is not a CompilationUnitSyntax.
        /// This covers the case where oldRoot is not CompilationUnitSyntax compilationUnit.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_NonCompilationUnitRoot_FixesHandlerMethod()
        {
            var source = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }





        /// <summary>
        /// Tests the AddCancellationTokenAsync method directly with a non-CompilationUnitSyntax root.
        /// This specifically tests the uncovered code path where oldRoot is not CompilationUnitSyntax.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenAsync_WithNonCompilationUnitRoot_CoversUncoveredBranch()
        {
            // Create a method declaration syntax directly (not wrapped in CompilationUnitSyntax)
            var methodDeclaration = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName("System.Threading.Tasks.ValueTask<string>"),
                "HandleAsync")
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("request"))
                        .WithType(SyntaxFactory.ParseTypeName("TestRequest")))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParseExpression("ValueTask"),
                                SyntaxFactory.IdentifierName("FromResult")))
                        .AddArgumentListArguments(
                            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("test")))))));

            // Create a document with just the method (this will still be CompilationUnitSyntax normally)
            var source = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> {|RELAY_GEN_207:HandleAsync|}(TestRequest request)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            var expected = @"
using System.Threading.Tasks;

namespace Relay.Core
{
    public interface IRequest { }
    public interface IRequest<out TResponse> { }
    
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HandleAttribute : System.Attribute
    {
        public string? Name { get; set; }
        public int Priority { get; set; }
    }
}

public class TestRequest : Relay.Core.IRequest<string> { }

public class TestHandler
{
    [Relay.Core.Handle]
    public ValueTask<string> HandleAsync(TestRequest request, System.Threading.CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(""test"");
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Helper method to verify code fix.
        /// </summary>
        private static async Task VerifyCodeFixAsync(string source, string expected)
        {
            var test = new CSharpCodeFixTest<RelayAnalyzer, RelayCodeFixProvider, XUnitVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", $@"root = true

[*]
build_property.EnableRelaySourceGenerator = true
"));

            await test.RunAsync();
        }
    }
}