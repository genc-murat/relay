using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;
using Moq;
using Xunit;

namespace Relay.MessageBroker.HealthChecks.Tests;

public class MessageBrokerHealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteDetailedResponse_WritesCorrectContentType()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task WriteDetailedResponse_ContainsStatus()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_ContainsTimestamp()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_ContainsTotalDuration()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("totalDuration", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_ContainsEntries()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("entries", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_IncludesEntryException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var entryWithException = new HealthReportEntry(
            HealthStatus.Unhealthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            exception,
            new Dictionary<string, object>(),
            Array.Empty<string>()
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithException }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        Assert.True(testCheck.TryGetProperty("exception", out var exceptionElement));
        Assert.Equal("Test exception", exceptionElement.GetProperty("message").GetString());
        Assert.Equal("InvalidOperationException", exceptionElement.GetProperty("type").GetString());
        // Stack trace might be null for exceptions that weren't actually thrown, so we just check if the property exists
        Assert.True(exceptionElement.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_IncludesEntryData()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var data = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true }
        };
        var entryWithData = new HealthReportEntry(
            HealthStatus.Healthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            null,
            data,
            Array.Empty<string>()
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithData }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        Assert.True(testCheck.TryGetProperty("data", out var dataElement));
        Assert.Equal("value1", dataElement.GetProperty("key1").GetString());
        Assert.Equal(42, dataElement.GetProperty("key2").GetInt32());
        Assert.True(dataElement.GetProperty("key3").GetBoolean());
    }

    [Fact]
    public async Task WriteDetailedResponse_IncludesEntryTags()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var tags = new[] { "tag1", "tag2", "tag3" };
        var entryWithTags = new HealthReportEntry(
            HealthStatus.Healthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            null,
            new Dictionary<string, object>(),
            tags
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithTags }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        Assert.True(testCheck.TryGetProperty("tags", out var tagsElement));
        var tagArray = tagsElement.EnumerateArray().ToList();
        Assert.Equal(3, tagArray.Count);
        Assert.Equal("tag1", tagArray[0].GetString());
        Assert.Equal("tag2", tagArray[1].GetString());
        Assert.Equal("tag3", tagArray[2].GetString());
    }

    [Fact]
    public async Task WriteDetailedResponse_HandlesNullException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var entryWithoutException = new HealthReportEntry(
            HealthStatus.Healthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            null, // No exception
            new Dictionary<string, object>(),
            Array.Empty<string>()
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithoutException }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        // Should not have an exception property when exception is null
        Assert.False(testCheck.TryGetProperty("exception", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_HandlesEmptyData()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var entryWithoutData = new HealthReportEntry(
            HealthStatus.Healthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            null,
            new Dictionary<string, object>(), // Empty data
            Array.Empty<string>()
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithoutData }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        // Should not have a data property when data is empty
        Assert.False(testCheck.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task WriteDetailedResponse_HandlesEmptyTags()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var entryWithoutTags = new HealthReportEntry(
            HealthStatus.Healthy,
            "Test description",
            TimeSpan.FromMilliseconds(100),
            null,
            new Dictionary<string, object>(),
            Array.Empty<string>() // Empty tags
        );
        var healthReport = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            { "test-check", entryWithoutTags }
        }, TimeSpan.FromMilliseconds(150));

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var entries = jsonDocument.RootElement.GetProperty("entries");
        var testCheck = entries.GetProperty("test-check");

        // Should not have a tags property when tags is empty
        Assert.False(testCheck.TryGetProperty("tags", out _));
    }

    [Fact]
    public async Task WriteSimpleResponse_WritesCorrectContentType()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse(httpContext, healthReport);

        // Assert
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task WriteSimpleResponse_ContainsStatus()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task WriteSimpleResponse_ContainsTimestamp()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task WriteSimpleResponse_ContainsChecks()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        Assert.True(jsonDocument.RootElement.TryGetProperty("checks", out _));
    }

    [Fact]
    public async Task WriteSimpleResponse_ChecksHaveExpectedProperties()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var healthReport = CreateHealthReport();

        // Act
        await MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse(httpContext, healthReport);

        // Assert
        var response = Encoding.UTF8.GetString(GetResponseBodyAsBytes(httpContext));
        var jsonDocument = JsonDocument.Parse(response);
        var checksElement = jsonDocument.RootElement.GetProperty("checks");
        var checkArray = checksElement.EnumerateArray().ToList();

        Assert.Single(checkArray); // We have one entry in our health report

        var check = checkArray[0];
        Assert.True(check.TryGetProperty("name", out _));
        Assert.True(check.TryGetProperty("status", out _));
        Assert.True(check.TryGetProperty("description", out _));
    }

    private HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        // Create a memory stream to capture the response
        var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;
        return context;
    }

    private byte[] GetResponseBodyAsBytes(HttpContext context)
    {
        var memoryStream = (MemoryStream)context.Response.Body;
        return memoryStream.ToArray();
    }

    private HealthReport CreateHealthReport()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "test-check", new HealthReportEntry(
                HealthStatus.Healthy,
                "Test description",
                TimeSpan.FromMilliseconds(100),
                null,
                new Dictionary<string, object>(),
                Array.Empty<string>()
            ) }
        };

        return new HealthReport(entries, TimeSpan.FromMilliseconds(150));
    }
}