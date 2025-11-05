using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

[Collection("Sequential")]
public class RelayTelemetryProviderNotificationActivityTests
{
    // Test to verify the branch where activity is not null and exception is not null
    [Fact]
    public void RecordNotificationPublish_WithActivityAndException_BranchCoverage()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));
        var testException = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, false, testException);

        // Assert - Verify that activity was properly modified
        // This test ensures the branch `if (exception != null)` is covered
        Assert.NotNull(activity); // Activity exists
        Assert.Equal(ActivityStatusCode.Error, activity.Status); // The activity should have status set to Error due to exception
    }

    // Test to verify the branch where activity is not null, exception is null, and success is true
    [Fact]
    public void RecordNotificationPublish_WithActivitySuccessNoException_BranchCoverage()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true, null);

        // Assert - Verify that activity was properly modified
        // This test ensures the branch `else if (success)` is covered
        Assert.NotNull(activity); // Activity exists
        Assert.Equal(ActivityStatusCode.Ok, activity.Status); // The activity should have status set to OK due to success = true
    }

    // Test to verify the branch where activity is not null, exception is null, and success is false
    [Fact]
    public void RecordNotificationPublish_WithActivityFailureNoException_BranchCoverage()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, false, null);

        // Assert - Verify that activity was properly modified
        // This test ensures the branch where none of the inner conditions are met is covered
        Assert.NotNull(activity); // Activity exists
        Assert.Equal(ActivityStatusCode.Unset, activity.Status); // The activity status should remain Unset since success = false and exception = null
    }

    // Test to verify the branch where activity is null
    [Fact]
    public void RecordNotificationPublish_WithNoCurrentActivity_BranchCoverage()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });
        var provider = new RelayTelemetryProvider(options);

        // Ensure no current activity
        Activity.Current = null;
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        var exception = Record.Exception(() =>
            provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true, null));

        // Assert - This test ensures the branch `if (activity != null)` is not taken
        Assert.Null(exception); // Should not throw
    }

    // Additional edge case tests to ensure comprehensive coverage
    [Fact]
    public void RecordNotificationPublish_WithActivityExceptionAndSuccessTrue_BranchCoverage()
    {
        // Arrange - This tests the scenario where success=true but exception is present
        // In this case, the first branch (exception != null) takes precedence
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new RelayTelemetryProvider(options);

        using var activity = provider.StartActivity("TestOperation", typeof(string));
        var testException = new ArgumentException("Argument exception");
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        provider.RecordNotificationPublish(typeof(TestNotification), 1, duration, true, testException);

        // Assert - Even with success=true, the exception branch should execute
        Assert.NotNull(activity); // Activity exists
        Assert.Equal(ActivityStatusCode.Error, activity.Status); // The activity status should be Error due to the exception taking precedence
    }
}
