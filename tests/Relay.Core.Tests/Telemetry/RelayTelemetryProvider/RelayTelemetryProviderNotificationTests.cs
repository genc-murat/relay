using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

[Collection("Sequential")]
public class RelayTelemetryProviderNotificationTests
{
    [Fact]
    public void RecordNotificationPublish_WithActivityAndException_Should_SetErrorStatus()
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

        // Assert - Activity should have error status and tags
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndExceptionAndSuccess_Should_SetErrorStatus()
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
        provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true, testException);

        // Assert - Even with success=true, if there's an exception, status should be Error
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test exception", activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndSuccess_Should_SetOkStatus()
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

        // Assert - Activity should have OK status
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Null(activity.StatusDescription); // OK status doesn't have a description
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndFailureNoException_Should_NotSetStatus()
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

        // Assert - When success is false and no exception, status should remain Unset
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        Assert.Null(activity.StatusDescription);
    }

    [Fact]
    public void RecordNotificationPublish_WhenActivityIsNull_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });
        var provider = new RelayTelemetryProvider(options);

        // Clear current activity
        Activity.Current = null;

        var duration = TimeSpan.FromMilliseconds(50);

        // Act & Assert - Should not throw when activity is null
        var exception = Record.Exception(() =>
            provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivity_SetTagsCorrectly()
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
        provider.RecordNotificationPublish(typeof(TestNotification), 2, duration, true);

        // Assert - Method should execute without error when activity exists
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndLargeHandlerCount_Should_Handle()
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
        provider.RecordNotificationPublish(typeof(TestNotification), int.MaxValue, duration, true);

        // Assert - Should handle large handler count without error
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public void RecordNotificationPublish_WithActivityAndZeroHandlerCount_Should_Handle()
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
        provider.RecordNotificationPublish(typeof(TestNotification), 0, duration, false);

        // Assert - Should handle zero handler count without error
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }
}