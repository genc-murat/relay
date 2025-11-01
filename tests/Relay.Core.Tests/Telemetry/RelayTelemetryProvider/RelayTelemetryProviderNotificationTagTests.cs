using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

[Collection("Sequential")]
public class RelayTelemetryProviderNotificationTagTests
{
    [Fact]
    public void RecordNotificationPublish_WithActivityAndException_SetsExpectedTagsAndStatus()
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

        // Assert - Check activity state after method execution
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndSuccess_SetsExpectedTagsAndStatus()
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
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true);

        // Assert - Check activity state after method execution
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Null(activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndFailureNoException_DoesNotSetStatus()
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
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, false);

        // Assert - Check activity state after method execution
        Assert.NotNull(activity);
        // When success is false and no exception, the status should remain unset (not changed)
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        Assert.Null(activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivitySuccessHasException_SetsErrorStatus()
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
        var testException = new ArgumentException("Argument error");
        var duration = TimeSpan.FromMilliseconds(50);

        // Act
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true, testException);

        // Assert - Even with success=true, the presence of an exception should set error status
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Argument error", activity.StatusDescription);
    }
}