using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Relay.MessageBroker.HealthChecks;

/// <summary>
/// Custom response writer for message broker health checks that provides detailed diagnostics.
/// </summary>
public static class MessageBrokerHealthCheckResponseWriter
{
    /// <summary>
    /// Writes a detailed health check response with diagnostics information.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report.</param>
    /// <returns>A task representing the write operation.</returns>
    public static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions
        {
            Indented = true
        };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", report.Status.ToString());
            jsonWriter.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("O"));
            jsonWriter.WriteNumber("totalDuration", report.TotalDuration.TotalMilliseconds);

            jsonWriter.WriteStartObject("entries");

            foreach (var entry in report.Entries)
            {
                jsonWriter.WriteStartObject(entry.Key);
                jsonWriter.WriteString("status", entry.Value.Status.ToString());
                jsonWriter.WriteString("description", entry.Value.Description);
                jsonWriter.WriteNumber("duration", entry.Value.Duration.TotalMilliseconds);

                if (entry.Value.Exception != null)
                {
                    jsonWriter.WriteStartObject("exception");
                    jsonWriter.WriteString("message", entry.Value.Exception.Message);
                    jsonWriter.WriteString("type", entry.Value.Exception.GetType().Name);
                    jsonWriter.WriteString("stackTrace", entry.Value.Exception.StackTrace);
                    jsonWriter.WriteEndObject();
                }

                if (entry.Value.Data.Count > 0)
                {
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in entry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);

                        if (item.Value == null)
                        {
                            jsonWriter.WriteNullValue();
                        }
                        else
                        {
                            JsonSerializer.Serialize(jsonWriter, item.Value, item.Value.GetType());
                        }
                    }

                    jsonWriter.WriteEndObject();
                }

                if (entry.Value.Tags.Any())
                {
                    jsonWriter.WriteStartArray("tags");
                    foreach (var tag in entry.Value.Tags)
                    {
                        jsonWriter.WriteStringValue(tag);
                    }
                    jsonWriter.WriteEndArray();
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
    }

    /// <summary>
    /// Writes a simple health check response with minimal information.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report.</param>
    /// <returns>A task representing the write operation.</returns>
    public static Task WriteSimpleResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return context.Response.WriteAsync(json);
    }
}
