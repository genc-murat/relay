using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Relay.Core.ContractValidation.Observability;

/// <summary>
/// Provides metrics for contract validation operations.
/// </summary>
public sealed class ContractValidationMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _validationCounter;
    private readonly Counter<long> _validationErrorCounter;
    private readonly Histogram<double> _validationDuration;
    private readonly Counter<long> _schemaResolutionCounter;
    private readonly Histogram<double> _schemaResolutionDuration;
    private readonly Counter<long> _customValidatorCounter;
    private readonly ObservableGauge<int> _activeValidations;
    private readonly ConcurrentDictionary<string, long> _errorCountsByType;
    private int _activeValidationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationMetrics"/> class.
    /// </summary>
    public ContractValidationMetrics()
    {
        _meter = new Meter("Relay.Core.ContractValidation", "1.0.0");
        _errorCountsByType = new ConcurrentDictionary<string, long>();

        // Validation counters
        _validationCounter = _meter.CreateCounter<long>(
            "contract_validation_total",
            description: "Total number of contract validations performed");

        _validationErrorCounter = _meter.CreateCounter<long>(
            "contract_validation_errors_total",
            description: "Total number of validation errors");

        // Validation duration histogram
        _validationDuration = _meter.CreateHistogram<double>(
            "contract_validation_duration_ms",
            unit: "ms",
            description: "Duration of contract validation operations in milliseconds");

        // Schema resolution metrics
        _schemaResolutionCounter = _meter.CreateCounter<long>(
            "schema_resolution_total",
            description: "Total number of schema resolution attempts");

        _schemaResolutionDuration = _meter.CreateHistogram<double>(
            "schema_resolution_duration_ms",
            unit: "ms",
            description: "Duration of schema resolution operations in milliseconds");

        // Custom validator metrics
        _customValidatorCounter = _meter.CreateCounter<long>(
            "custom_validator_executions_total",
            description: "Total number of custom validator executions");

        // Active validations gauge
        _activeValidations = _meter.CreateObservableGauge(
            "contract_validation_active",
            () => _activeValidationCount,
            description: "Number of currently active validation operations");
    }

    /// <summary>
    /// Records a validation operation.
    /// </summary>
    /// <param name="requestType">The type of request being validated.</param>
    /// <param name="isRequest">Whether this is a request (true) or response (false) validation.</param>
    /// <param name="isValid">Whether the validation passed.</param>
    /// <param name="durationMs">The duration of the validation in milliseconds.</param>
    /// <param name="errorCount">The number of validation errors.</param>
    public void RecordValidation(
        string requestType,
        bool isRequest,
        bool isValid,
        double durationMs,
        int errorCount = 0)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("request_type", requestType),
            new("validation_target", isRequest ? "request" : "response"),
            new("is_valid", isValid)
        };

        _validationCounter.Add(1, tags);
        _validationDuration.Record(durationMs, tags);

        if (!isValid && errorCount > 0)
        {
            _validationErrorCounter.Add(errorCount, tags);
        }
    }

    /// <summary>
    /// Records a schema resolution operation.
    /// </summary>
    /// <param name="typeName">The type name for which schema was resolved.</param>
    /// <param name="providerType">The provider type that resolved the schema.</param>
    /// <param name="success">Whether the resolution was successful.</param>
    /// <param name="durationMs">The duration of the resolution in milliseconds.</param>
    public void RecordSchemaResolution(
        string typeName,
        string? providerType,
        bool success,
        double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("type_name", typeName),
            new("provider_type", providerType ?? "unknown"),
            new("success", success)
        };

        _schemaResolutionCounter.Add(1, tags);
        _schemaResolutionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a custom validator execution.
    /// </summary>
    /// <param name="validatorType">The type of custom validator.</param>
    /// <param name="success">Whether the validator executed successfully.</param>
    public void RecordCustomValidatorExecution(string validatorType, bool success)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("validator_type", validatorType),
            new("success", success)
        };

        _customValidatorCounter.Add(1, tags);
    }

    /// <summary>
    /// Records a validation error by type.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    public void RecordValidationError(string errorCode)
    {
        _errorCountsByType.AddOrUpdate(errorCode, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Increments the active validation count.
    /// </summary>
    public void IncrementActiveValidations()
    {
        System.Threading.Interlocked.Increment(ref _activeValidationCount);
    }

    /// <summary>
    /// Decrements the active validation count.
    /// </summary>
    public void DecrementActiveValidations()
    {
        System.Threading.Interlocked.Decrement(ref _activeValidationCount);
    }

    /// <summary>
    /// Gets the error counts by type.
    /// </summary>
    public ConcurrentDictionary<string, long> GetErrorCountsByType() => _errorCountsByType;

    /// <inheritdoc />
    public void Dispose()
    {
        _meter?.Dispose();
    }
}
