using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Advanced testing framework for Relay-based applications.
/// Provides scenario-based testing, load testing, and behavior verification.
/// </summary>
public class RelayTestFramework(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IRelay _relay = (IRelay)serviceProvider.GetRequiredService(typeof(IRelay));
    private readonly ILogger<RelayTestFramework>? _logger = (ILogger<RelayTestFramework>?)serviceProvider.GetService(typeof(ILogger<RelayTestFramework>));
    private readonly List<TestScenario> _scenarios = new();

    /// <summary>
    /// Creates a new test scenario.
    /// </summary>
    public TestScenarioBuilder Scenario(string name)
    {
        var scenario = new TestScenario { Name = name };
        _scenarios.Add(scenario);
        return new TestScenarioBuilder(scenario, _relay);
    }

    /// <summary>
    /// Runs all configured scenarios.
    /// </summary>
    public async Task<TestRunResult> RunAllScenariosAsync(CancellationToken cancellationToken = default)
    {
        var result = new TestRunResult { StartedAt = DateTime.UtcNow };

        foreach (var scenario in _scenarios)
        {
            var scenarioResult = await RunScenarioAsync(scenario, cancellationToken);
            result.ScenarioResults.Add(scenarioResult);
        }

        result.CompletedAt = DateTime.UtcNow;
        result.TotalDuration = result.CompletedAt.Value - result.StartedAt;

        return result;
    }

    /// <summary>
    /// Runs load testing against specified request types.
    /// </summary>
    public async Task<LoadTestResult> RunLoadTestAsync<TRequest>(
        TRequest request,
        LoadTestConfiguration config,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        ArgumentNullException.ThrowIfNull(config);
        ValidateLoadTestConfiguration(config);

        var result = new LoadTestResult
        {
            RequestType = typeof(TRequest).Name,
            StartedAt = DateTime.UtcNow,
            Configuration = config
        };

        _logger?.LogInformation("Starting load test for {RequestType} with {TotalRequests} requests, max concurrency {MaxConcurrency}",
            typeof(TRequest).Name, config.TotalRequests, config.MaxConcurrency);

        var semaphore = new SemaphoreSlim(config.MaxConcurrency, config.MaxConcurrency);
        var tasks = new List<Task>();

        for (int i = 0; i < config.TotalRequests; i++)
        {
            tasks.Add(ExecuteLoadTestRequestAsync(request, semaphore, result, cancellationToken));

            if (config.RampUpDelayMs > 0)
            {
                await Task.Delay(config.RampUpDelayMs, cancellationToken);
            }
        }

        await Task.WhenAll(tasks);

        result.CompletedAt = DateTime.UtcNow;
        result.TotalDuration = result.CompletedAt.Value - result.StartedAt;
        result.AverageResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.Average() : 0;
        result.MedianResponseTime = CalculateMedian(result.ResponseTimes);
        result.P95ResponseTime = CalculatePercentile(result.ResponseTimes, 0.95);
        result.P99ResponseTime = CalculatePercentile(result.ResponseTimes, 0.99);

        _logger?.LogInformation("Load test completed for {RequestType}. Success rate: {SuccessRate:P2}, Avg response time: {AvgResponseTime:F2}ms",
            typeof(TRequest).Name, result.SuccessRate, result.AverageResponseTime);

        return result;
    }

    private async Task ExecuteLoadTestRequestAsync<TRequest>(
        TRequest request,
        SemaphoreSlim semaphore,
        LoadTestResult result,
        CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await _relay.SendAsync((IRequest)request, cancellationToken);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                lock (result.ResponseTimes)
                {
                    result.ResponseTimes.Add(duration);
                    result.SuccessfulRequests++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Load test request failed for {RequestType}", typeof(TRequest).Name);
                lock (result.ResponseTimes)
                {
                    result.FailedRequests++;
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<ScenarioResult> RunScenarioAsync(TestScenario scenario, CancellationToken cancellationToken)
    {
        var result = new ScenarioResult
        {
            ScenarioName = scenario.Name,
            StartedAt = DateTime.UtcNow,
            Success = true
        };

        try
        {
            foreach (var step in scenario.Steps)
            {
                var stepResult = await ExecuteStepAsync(step, cancellationToken);
                result.StepResults.Add(stepResult);
                
                if (!stepResult.Success)
                {
                    result.Success = false;
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    private async Task<StepResult> ExecuteStepAsync(TestStep step, CancellationToken cancellationToken)
    {
        step.Validate();

        var result = new StepResult
        {
            StepName = step.Name,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger?.LogDebug("Executing step: {StepName} ({StepType})", step.Name, step.Type);

            switch (step.Type)
            {
                case StepType.SendRequest:
                    await ExecuteSendRequestStep(step, result, cancellationToken);
                    break;
                case StepType.PublishNotification:
                    await ExecutePublishNotificationStep(step, result, cancellationToken);
                    break;
                case StepType.StreamRequest:
                    await ExecuteStreamRequestStep(step, result, cancellationToken);
                    break;
                case StepType.Verify:
                    await ExecuteVerifyStep(step, result, cancellationToken);
                    break;
                case StepType.Wait:
                    await Task.Delay(step.WaitTime ?? TimeSpan.FromSeconds(1), cancellationToken);
                    break;
            }

            result.Success = true;
            _logger?.LogDebug("Step completed successfully: {StepName}", step.Name);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger?.LogError(ex, "Step failed: {StepName}", step.Name);
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    private async Task ExecuteSendRequestStep(TestStep step, StepResult result, CancellationToken cancellationToken)
    {
        if (step.Request == null)
            throw new InvalidOperationException("Request is required for SendRequest step");

        // Use the non-generic SendAsync method
        await _relay.SendAsync((IRequest)step.Request, cancellationToken);
    }

    private async Task ExecutePublishNotificationStep(TestStep step, StepResult result, CancellationToken cancellationToken)
    {
        if (step.Notification == null)
            throw new InvalidOperationException("Notification is required for PublishNotification step");

        // Use reflection to invoke the correct generic PublishAsync method
        var notificationType = step.Notification.GetType();
        var publishMethod = (typeof(IRelay).GetMethod(nameof(IRelay.PublishAsync))?.MakeGenericMethod(notificationType)) ?? throw new InvalidOperationException($"Cannot find PublishAsync method for notification type {notificationType}");
        dynamic task = publishMethod.Invoke(_relay, new object[] { step.Notification, cancellationToken })!;
        await task;
    }

    private async Task ExecuteStreamRequestStep(TestStep step, StepResult result, CancellationToken cancellationToken)
    {
        if (step.StreamRequest == null)
            throw new InvalidOperationException("StreamRequest is required for StreamRequest step");

        // Use reflection to invoke the correct generic StreamAsync method
        var requestType = step.StreamRequest.GetType();
        var responseType = requestType.GetGenericArguments()[0];
        var streamMethod = (typeof(IRelay).GetMethod(nameof(IRelay.StreamAsync))?.MakeGenericMethod(responseType)) ?? throw new InvalidOperationException($"Cannot find StreamAsync method for request type {requestType}");
        var enumerable = streamMethod.Invoke(_relay, new object[] { step.StreamRequest, cancellationToken });

        // Use reflection to iterate over the async enumerable
        var enumeratorMethod = enumerable!.GetType().GetMethod("GetAsyncEnumerator") ?? throw new InvalidOperationException("Cannot get async enumerator for stream");
        var enumerator = enumeratorMethod.Invoke(enumerable, new object[] { cancellationToken }) ?? throw new InvalidOperationException("Cannot get async enumerator");
        try
        {
            var moveNextMethod = enumerator.GetType().GetMethod("MoveNextAsync") ?? throw new InvalidOperationException("Cannot move next on async enumerator");
            while (true)
            {
                var invokeResult = moveNextMethod.Invoke(enumerator, Array.Empty<object>());
                if (invokeResult is ValueTask<bool> moveNextTask)
                {
                    if (!await moveNextTask)
                        break;
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            var disposeMethod = enumerator.GetType().GetMethod("DisposeAsync");
            if (disposeMethod != null)
            {
                var invokeResult = disposeMethod.Invoke(enumerator, Array.Empty<object>());
                if (invokeResult is ValueTask disposeTask)
                {
                    await disposeTask;
                }
            }
        }
    }

    private async Task ExecuteVerifyStep(TestStep step, StepResult result, CancellationToken cancellationToken)
    {
        if (step.VerificationFunc == null)
            throw new InvalidOperationException("VerificationFunc is required for Verify step");

        var isValid = await step.VerificationFunc();
        if (!isValid)
        {
            throw new VerificationException($"Verification failed for step: {step.Name}");
        }
    }

    private void ValidateLoadTestConfiguration(LoadTestConfiguration config)
    {
        if (config.TotalRequests <= 0)
            throw new ArgumentException("TotalRequests must be greater than 0", nameof(config.TotalRequests));
        if (config.MaxConcurrency <= 0)
            throw new ArgumentException("MaxConcurrency must be greater than 0", nameof(config.MaxConcurrency));
        if (config.RampUpDelayMs < 0)
            throw new ArgumentException("RampUpDelayMs cannot be negative", nameof(config.RampUpDelayMs));
    }

    private double CalculateMedian(List<double> values)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        if (percentile < 0 || percentile > 1)
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 1");

        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));

        return sorted[index];
    }
}