 using System;
 using System.Collections.Generic;
 using System.Linq;
 using Microsoft.Extensions.Options;
 using Relay.Core.Configuration.Options.Core;
 using Relay.Core.Configuration.Options.Endpoints;
 using Relay.Core.Configuration.Options.Handlers;
 using Relay.Core.Configuration.Options.Notifications;

namespace Relay.Core.Configuration.Core;

/// <summary>
/// Validator for Relay configuration options.
/// </summary>
public class RelayOptionsValidator : IValidateOptions<RelayOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RelayOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("RelayOptions cannot be null.");
        }

        var failures = new List<string>();

        // Validate global defaults
        ValidateHandlerOptions(options.DefaultHandlerOptions, "DefaultHandlerOptions", failures);
        ValidateNotificationOptions(options.DefaultNotificationOptions, "DefaultNotificationOptions", failures);
        ValidatePipelineOptions(options.DefaultPipelineOptions, "DefaultPipelineOptions", failures);
        ValidateEndpointOptions(options.DefaultEndpointOptions, "DefaultEndpointOptions", failures);

        // Validate global settings
        if (options.MaxConcurrentNotificationHandlers <= 0)
        {
            failures.Add("MaxConcurrentNotificationHandlers must be greater than 0.");
        }

        // Validate handler overrides
        foreach (var kvp in options.HandlerOverrides)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                failures.Add("Handler override keys cannot be null or empty.");
                continue;
            }

            ValidateHandlerOptions(kvp.Value, $"HandlerOverrides[{kvp.Key}]", failures);
        }

        // Validate notification overrides
        foreach (var kvp in options.NotificationOverrides)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                failures.Add("Notification override keys cannot be null or empty.");
                continue;
            }

            ValidateNotificationOptions(kvp.Value, $"NotificationOverrides[{kvp.Key}]", failures);
        }

        // Validate pipeline overrides
        foreach (var kvp in options.PipelineOverrides)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                failures.Add("Pipeline override keys cannot be null or empty.");
                continue;
            }

            ValidatePipelineOptions(kvp.Value, $"PipelineOverrides[{kvp.Key}]", failures);
        }

        return failures.Any()
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateHandlerOptions(HandlerOptions options, string path, List<string> failures)
    {
        if (options == null)
        {
            failures.Add($"{path} cannot be null.");
            return;
        }

        if (options.DefaultTimeout.HasValue && options.DefaultTimeout.Value <= TimeSpan.Zero)
        {
            failures.Add($"{path}.DefaultTimeout must be greater than zero when specified.");
        }

        if (options.MaxRetryAttempts < 0)
        {
            failures.Add($"{path}.MaxRetryAttempts cannot be negative.");
        }

        if (options.EnableRetry && options.MaxRetryAttempts == 0)
        {
            failures.Add($"{path}.MaxRetryAttempts must be greater than 0 when EnableRetry is true.");
        }
    }

    private static void ValidateNotificationOptions(NotificationOptions options, string path, List<string> failures)
    {
        if (options == null)
        {
            failures.Add($"{path} cannot be null.");
            return;
        }

        if (!Enum.IsDefined(typeof(NotificationDispatchMode), options.DefaultDispatchMode))
        {
            failures.Add($"{path}.DefaultDispatchMode has an invalid value.");
        }

        if (options.DefaultTimeout.HasValue && options.DefaultTimeout.Value <= TimeSpan.Zero)
        {
            failures.Add($"{path}.DefaultTimeout must be greater than zero when specified.");
        }

        if (options.MaxDegreeOfParallelism <= 0)
        {
            failures.Add($"{path}.MaxDegreeOfParallelism must be greater than 0.");
        }
    }

    private static void ValidatePipelineOptions(PipelineOptions options, string path, List<string> failures)
    {
        if (options == null)
        {
            failures.Add($"{path} cannot be null.");
            return;
        }

        if (!Enum.IsDefined(typeof(PipelineScope), options.DefaultScope))
        {
            failures.Add($"{path}.DefaultScope has an invalid value.");
        }

        if (options.DefaultTimeout.HasValue && options.DefaultTimeout.Value <= TimeSpan.Zero)
        {
            failures.Add($"{path}.DefaultTimeout must be greater than zero when specified.");
        }
    }

    private static void ValidateEndpointOptions(EndpointOptions options, string path, List<string> failures)
    {
        if (options == null)
        {
            failures.Add($"{path} cannot be null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.DefaultHttpMethod))
        {
            failures.Add($"{path}.DefaultHttpMethod cannot be null or empty.");
        }
        else
        {
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            if (!validMethods.Contains(options.DefaultHttpMethod.ToUpperInvariant()))
            {
                failures.Add($"{path}.DefaultHttpMethod must be a valid HTTP method.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultRoutePrefix))
        {
            if (options.DefaultRoutePrefix.Contains("//"))
            {
                failures.Add($"{path}.DefaultRoutePrefix cannot contain consecutive slashes.");
            }

            if (options.DefaultRoutePrefix.StartsWith("/") && options.DefaultRoutePrefix.Length > 1)
            {
                failures.Add($"{path}.DefaultRoutePrefix should not start with a slash unless it's the root path.");
            }
        }
    }
}
