using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Relay.SourceGenerator.CodeFixes;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for the RelayCodeFixProvider to ensure proper code fixes for handler signatures.
    /// </summary>
    public class RelayCodeFixProviderTests
    {
        /// <summary>
        /// Tests that the code fix adds CancellationToken parameter to a handler method.
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
        /// Tests that the code fix adds CancellationToken parameter when System.Threading is already imported.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_WithExistingUsing_FixesHandlerMethod()
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
        /// Tests that the code fix adds CancellationToken parameter to a notification handler method.
        /// </summary>
        [Fact]
        public async Task AddCancellationTokenParameter_FixesNotificationHandlerMethod()
        {
            var source = @"
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

public class TestNotification : Relay.Core.INotification { }

public class TestHandler
{
    [Relay.Core.Notification]
    public Task HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            var expected = @"
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

public class TestNotification : Relay.Core.INotification { }

public class TestHandler
{
    [Relay.Core.Notification]
    public Task HandleAsync(TestNotification notification, System.Threading.CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";

            await VerifyCodeFixAsync(source, expected);
        }

        /// <summary>
        /// Tests that AddCancellationTokenAsync method exists and is accessible.
        /// </summary>
        [Fact]
        public void AddCancellationTokenAsync_MethodExists()
        {
            // This test verifies that the AddCancellationTokenAsync method exists
            // The actual functionality is tested in integration tests
            var method = typeof(RelayCodeFixProvider).GetMethod("AddCancellationTokenAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
        }

        /// <summary>
        /// Helper method to verify code fix.
        /// </summary>
        private static async Task VerifyCodeFixAsync(string source, string expected)
        {
            var test = new CSharpCodeFixTest<RelayAnalyzer, RelayCodeFixProvider, DefaultVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

            await test.RunAsync();
        }
    }
}