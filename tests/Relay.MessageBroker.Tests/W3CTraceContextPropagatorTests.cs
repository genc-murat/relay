using Relay.MessageBroker.DistributedTracing;
using System.Diagnostics;
using System.Text;

namespace Relay.MessageBroker.Tests;

public class W3CTraceContextPropagatorTests
{
    [Fact]
    public void Inject_ShouldDoNothing_WhenActivityIsNull()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        Activity? activity = null;

        // Act
        W3CTraceContextPropagator.Inject(headers, activity);

        // Assert
        Assert.Empty(headers);
    }

    [Fact]
    public void Inject_ShouldAddTraceParentHeader_WhenActivityExists()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);

            // Assert
            Assert.True(headers.ContainsKey("traceparent"));
            var traceParent = headers["traceparent"].ToString();
            Assert.NotNull(traceParent);
            Assert.StartsWith("00-", traceParent);
            // Verify format: version-traceId-spanId-traceFlags
            var parts = traceParent.Split('-');
            Assert.Equal(4, parts.Length);
            Assert.Equal("00", parts[0]);
            Assert.Equal(32, parts[1].Length); // traceId length
            Assert.Equal(16, parts[2].Length); // spanId length
            // Check if recorded flag is set (may be 00 or 01 depending on activity state)
            Assert.True(parts[3] == "01" || parts[3] == "00");
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Inject_ShouldAddTraceParentHeaderWithNotRecordedFlag_WhenActivityNotRecorded()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.ActivityTraceFlags = ActivityTraceFlags.None;
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);

            // Assert
            var traceParent = headers["traceparent"].ToString();
            Assert.EndsWith("-00", traceParent); // Not recorded flag
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Inject_ShouldAddTraceStateHeader_WhenActivityHasTraceState()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.TraceStateString = "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE";
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);

            // Assert
            Assert.True(headers.ContainsKey("tracestate"));
            Assert.Equal("rojo=00f067aa0ba902b7,congo=t61rcWkgMzE", headers["tracestate"]);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Inject_ShouldNotAddTraceStateHeader_WhenActivityHasNoTraceState()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);

            // Assert
            Assert.False(headers.ContainsKey("tracestate"));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Inject_ShouldNotAddTraceStateHeader_WhenActivityTraceStateIsEmpty()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.TraceStateString = string.Empty;
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);

            // Assert
            Assert.False(headers.ContainsKey("tracestate"));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenHeadersIsNull()
    {
        // Act
        var result = W3CTraceContextPropagator.Extract(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenHeadersDoesNotContainTraceParent()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["otherheader"] = "value"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceParentValueIsNull()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = null!
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceParentValueIsEmpty()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = string.Empty
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceParentHasInvalidFormat()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = "invalid-format"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceParentHasTooFewParts()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = "00-traceid-spanid"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceParentHasTooManyParts()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = "00-traceid-spanid-01-extra"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenTraceIdIsInvalid()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = "00-invalid-traceid-spanid-01"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnNull_WhenSpanIdIsInvalid()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-invalid-spanid-01"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_ShouldReturnValidTraceContext_WhenTraceParentIsValid()
    {
        // Arrange
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var spanId = "b7ad6b7169203331";
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = $"00-{traceId}-{spanId}-01"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(traceId, result.Value.TraceId.ToHexString());
        Assert.Equal(spanId, result.Value.SpanId.ToHexString());
        Assert.Equal(ActivityTraceFlags.Recorded, result.Value.TraceFlags);
    }

    [Fact]
    public void Extract_ShouldReturnValidTraceContextWithNotRecordedFlag_WhenTraceFlagsAre00()
    {
        // Arrange
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var spanId = "b7ad6b7169203331";
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = $"00-{traceId}-{spanId}-00"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(traceId, result.Value.TraceId.ToHexString());
        Assert.Equal(spanId, result.Value.SpanId.ToHexString());
        Assert.Equal(ActivityTraceFlags.None, result.Value.TraceFlags);
    }

    [Fact]
    public void Extract_ShouldHandleNonStringTraceParentValue()
    {
        // Arrange
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var spanId = "b7ad6b7169203331";
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = new StringBuilder($"00-{traceId}-{spanId}-01")
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(traceId, result.Value.TraceId.ToHexString());
        Assert.Equal(spanId, result.Value.SpanId.ToHexString());
        Assert.Equal(ActivityTraceFlags.Recorded, result.Value.TraceFlags);
    }

    [Fact]
    public void ExtractTraceState_ShouldReturnNull_WhenHeadersIsNull()
    {
        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTraceState_ShouldReturnNull_WhenHeadersDoesNotContainTraceState()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["otherheader"] = "value"
        };

        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTraceState_ShouldReturnNull_WhenTraceStateValueIsNull()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["tracestate"] = null!
        };

        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractTraceState_ShouldReturnTraceState_WhenTraceStateExists()
    {
        // Arrange
        var traceState = "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE";
        var headers = new Dictionary<string, object>
        {
            ["tracestate"] = traceState
        };

        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(headers);

        // Assert
        Assert.Equal(traceState, result);
    }

    [Fact]
    public void ExtractTraceState_ShouldHandleNonStringTraceStateValue()
    {
        // Arrange
        var traceState = "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE";
        var headers = new Dictionary<string, object>
        {
            ["tracestate"] = new StringBuilder(traceState)
        };

        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(headers);

        // Assert
        Assert.Equal(traceState, result);
    }

    [Fact]
    public void ExtractTraceState_ShouldReturnEmptyString_WhenTraceStateIsEmpty()
    {
        // Arrange
        var headers = new Dictionary<string, object>
        {
            ["tracestate"] = string.Empty
        };

        // Act
        var result = W3CTraceContextPropagator.ExtractTraceState(headers);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTraceContext()
    {
        // Arrange
        var originalHeaders = new Dictionary<string, object>();
        var activity = new Activity("TestActivity");
        activity.TraceStateString = "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE";
        activity.Start();

        try
        {
            // Act - Inject trace context
            W3CTraceContextPropagator.Inject(originalHeaders, activity);

            // Extract trace context
            var extractedContext = W3CTraceContextPropagator.Extract(originalHeaders);
            var extractedTraceState = W3CTraceContextPropagator.ExtractTraceState(originalHeaders);

            // Assert
            Assert.NotNull(extractedContext);
            Assert.Equal(activity.TraceId, extractedContext.Value.TraceId);
            Assert.Equal(activity.SpanId, extractedContext.Value.SpanId);
            Assert.Equal(activity.ActivityTraceFlags, extractedContext.Value.TraceFlags);
            Assert.Equal(activity.TraceStateString, extractedTraceState);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Inject_Extract_ShouldWorkWithMinimalActivity()
    {
        // Arrange
        var headers = new Dictionary<string, object>();
        var activity = new Activity("MinimalActivity");
        activity.Start();

        try
        {
            // Act
            W3CTraceContextPropagator.Inject(headers, activity);
            var extracted = W3CTraceContextPropagator.Extract(headers);

            // Assert
            Assert.NotNull(extracted);
            Assert.Equal(activity.TraceId, extracted.Value.TraceId);
            Assert.Equal(activity.SpanId, extracted.Value.SpanId);
            Assert.Equal(activity.ActivityTraceFlags, extracted.Value.TraceFlags);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Theory]
    [InlineData("01")]
    [InlineData("00")]
    [InlineData("11")]
    [InlineData("10")]
    public void Extract_ShouldHandleVariousTraceFlags(string traceFlags)
    {
        // Arrange
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var spanId = "b7ad6b7169203331";
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = $"00-{traceId}-{spanId}-{traceFlags}"
        };

        // Act
        var result = W3CTraceContextPropagator.Extract(headers);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(traceId, result.Value.TraceId.ToHexString());
        Assert.Equal(spanId, result.Value.SpanId.ToHexString());
        
        var expectedFlags = traceFlags == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
        Assert.Equal(expectedFlags, result.Value.TraceFlags);
    }
}