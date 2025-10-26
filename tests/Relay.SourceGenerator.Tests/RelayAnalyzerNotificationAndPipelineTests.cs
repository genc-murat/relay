extern alias RelayCore;
namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for the RelayAnalyzer to ensure coverage of Notification and Pipeline attribute branches
/// in the AnalyzeMethodDeclaration method.
/// </summary>
public class RelayAnalyzerNotificationAndPipelineTests
{
    /// <summary>
    /// Tests that RelayAnalyzer calls NotificationHandlerValidator when [Notification] attribute is present.
    /// This covers the 'if (notificationAttribute != null)' branch in AnalyzeMethodDeclaration.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_WithNotificationAttribute_CallsNotificationValidator()
    {
        // Arrange - Valid notification handler should not produce diagnostics
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestNotification : INotification { }
    
    public class TestHandler
    {
        [Notification]
        public async Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1);
        }
    }
}";

        // Act & Assert - This should not throw and should validate the notification handler
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that RelayAnalyzer calls PipelineValidator when [Pipeline] attribute is present.
    /// This covers the 'if (pipelineAttribute != null)' branch in AnalyzeMethodDeclaration.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_WithPipelineAttribute_CallsPipelineValidator()
    {
        // Arrange - Valid pipeline handler should not produce diagnostics
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
    
    public class TestHandler
    {
        [Pipeline]
        public async Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
        {
            return await next();
        }
    }
}";

        // Act & Assert - This should not throw and should validate the pipeline handler
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that RelayAnalyzer properly handles a method with only [Notification] attribute.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_WithOnlyNotificationAttribute()
    {
        // Arrange - Valid notification handler should not produce diagnostics
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestNotification : INotification { }
    
    public class TestHandler
    {
        [Notification]
        public Task HandleNotificationAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}";

        // Act & Assert
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that RelayAnalyzer properly handles a method with only [Pipeline] attribute.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_WithOnlyPipelineAttribute()
    {
        // Arrange - Valid pipeline handler should not produce diagnostics
        var source = @"
using System.Threading;
using System.Threading.Tasks;
using RelayCore;

namespace TestProject
{
    public class TestRequest : IRequest<string> { }
    
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
    
    public class TestHandler
    {
        [Pipeline]
        public async ValueTask<string> ProcessAsync(TestRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
        {
            var result = await next(cancellationToken);
            return result;
        }
    }
}";

        // Act & Assert
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// Tests that RelayAnalyzer handles multiple Relay attributes on the same method correctly.
    /// This tests that all attribute validation branches execute properly.
    /// </summary>
    [Fact]
    public async Task RelayAnalyzer_AnalyzeMethodDeclaration_WithMultipleRelayAttributes()
    {
        // Arrange - Multiple Relay attributes on one method should produce a diagnostic
        // Using Handle and Notification together similar to the existing test
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

        // Act & Assert - This should validate both attributes and report a diagnostic about invalid request parameter
        await RelayAnalyzerTestHelpers.VerifyAnalyzerAsync(source);
    }
}