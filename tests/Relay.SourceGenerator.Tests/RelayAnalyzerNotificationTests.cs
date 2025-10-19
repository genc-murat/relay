extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for notification handler validation in the RelayAnalyzer.
    /// </summary>
    public class RelayAnalyzerNotificationTests
    {
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }

        /// <summary>
        /// Tests that notification handlers missing CancellationToken produce warnings.
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
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

            await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
        }
    }
}