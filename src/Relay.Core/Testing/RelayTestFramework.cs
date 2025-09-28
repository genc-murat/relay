using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Testing
{
    /// <summary>
    /// Advanced testing framework for Relay-based applications.
    /// Provides scenario-based testing, load testing, and behavior verification.
    /// </summary>
    public class RelayTestFramework
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRelay _relay;
        private readonly List<TestScenario> _scenarios = new();

        public RelayTestFramework(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _relay = serviceProvider.GetRequiredService<IRelay>();
        }

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
            var result = new LoadTestResult
            {
                RequestType = typeof(TRequest).Name,
                StartedAt = DateTime.UtcNow,
                Configuration = config
            };

            var semaphore = new SemaphoreSlim(config.MaxConcurrency, config.MaxConcurrency);
            var tasks = new List<Task>();

            for (int i = 0; i < config.TotalRequests; i++)
            {
                tasks.Add(ExecuteLoadTestRequest(request, semaphore, result, cancellationToken));
                
                if (config.RampUpDelayMs > 0)
                {
                    await Task.Delay(config.RampUpDelayMs, cancellationToken);
                }
            }

            await Task.WhenAll(tasks);

            result.CompletedAt = DateTime.UtcNow;
            result.TotalDuration = result.CompletedAt.Value - result.StartedAt;
            result.AverageResponseTime = result.ResponseTimes.Any() ? result.ResponseTimes.Average() : 0;
            result.MedianResponseTime = CalculateMedian(result.ResponseTimes);
            result.P95ResponseTime = CalculatePercentile(result.ResponseTimes, 0.95);
            result.P99ResponseTime = CalculatePercentile(result.ResponseTimes, 0.99);

            return result;
        }

        private async Task ExecuteLoadTestRequest<TRequest>(
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
                    await _relay.SendAsync(request, cancellationToken);
                    
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    lock (result.ResponseTimes)
                    {
                        result.ResponseTimes.Add(duration);
                        result.SuccessfulRequests++;
                    }
                }
                catch (Exception)
                {
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
                StartedAt = DateTime.UtcNow
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
                        break;
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
            var result = new StepResult
            {
                StepName = step.Name,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                switch (step.Type)
                {
                    case StepType.SendRequest:
                        await ExecuteSendRequestStep(step, result, cancellationToken);
                        break;
                    case StepType.PublishNotification:
                        await ExecutePublishNotificationStep(step, result, cancellationToken);
                        break;
                    case StepType.Verify:
                        await ExecuteVerifyStep(step, result, cancellationToken);
                        break;
                    case StepType.Wait:
                        await Task.Delay(step.WaitTime ?? TimeSpan.FromSeconds(1), cancellationToken);
                        break;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        private async Task ExecuteSendRequestStep(TestStep step, StepResult result, CancellationToken cancellationToken)
        {
            if (step.Request == null)
                throw new InvalidOperationException("Request is required for SendRequest step");

            result.Response = await _relay.SendAsync((dynamic)step.Request, cancellationToken);
        }

        private async Task ExecutePublishNotificationStep(TestStep step, StepResult result, CancellationToken cancellationToken)
        {
            if (step.Notification == null)
                throw new InvalidOperationException("Notification is required for PublishNotification step");

            await _relay.PublishAsync((dynamic)step.Notification, cancellationToken);
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

        private double CalculateMedian(List<double> values)
        {
            if (!values.Any()) return 0;
            
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
            if (!values.Any()) return 0;
            
            var sorted = values.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(index, sorted.Count - 1));
            
            return sorted[index];
        }
    }

    /// <summary>
    /// Builder for test scenarios.
    /// </summary>
    public class TestScenarioBuilder
    {
        private readonly TestScenario _scenario;
        private readonly IRelay _relay;

        public TestScenarioBuilder(TestScenario scenario, IRelay relay)
        {
            _scenario = scenario;
            _relay = relay;
        }

        public TestScenarioBuilder SendRequest<TRequest>(TRequest request, string stepName = "Send Request")
            where TRequest : IRequest
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.SendRequest,
                Request = request
            });
            return this;
        }

        public TestScenarioBuilder PublishNotification<TNotification>(TNotification notification, string stepName = "Publish Notification")
            where TNotification : INotification
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.PublishNotification,
                Notification = notification
            });
            return this;
        }

        public TestScenarioBuilder Verify(Func<Task<bool>> verificationFunc, string stepName = "Verify")
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.Verify,
                VerificationFunc = verificationFunc
            });
            return this;
        }

        public TestScenarioBuilder Wait(TimeSpan duration, string stepName = "Wait")
        {
            _scenario.Steps.Add(new TestStep
            {
                Name = stepName,
                Type = StepType.Wait,
                WaitTime = duration
            });
            return this;
        }
    }

    // Supporting classes and enums for the test framework
    public class TestScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<TestStep> Steps { get; set; } = new();
    }

    public class TestStep
    {
        public string Name { get; set; } = string.Empty;
        public StepType Type { get; set; }
        public object? Request { get; set; }
        public object? Notification { get; set; }
        public Func<Task<bool>>? VerificationFunc { get; set; }
        public TimeSpan? WaitTime { get; set; }
    }

    public enum StepType
    {
        SendRequest,
        PublishNotification,
        Verify,
        Wait
    }

    public class TestRunResult
    {
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<ScenarioResult> ScenarioResults { get; set; } = new();
        public bool Success => ScenarioResults.All(s => s.Success);
    }

    public class ScenarioResult
    {
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
    }

    public class StepResult
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Response { get; set; }
    }

    public class LoadTestConfiguration
    {
        public int TotalRequests { get; set; } = 100;
        public int MaxConcurrency { get; set; } = 10;
        public int RampUpDelayMs { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);
    }

    public class LoadTestResult
    {
        public string RequestType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public LoadTestConfiguration Configuration { get; set; } = new();
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public List<double> ResponseTimes { get; set; } = new();
        public double AverageResponseTime { get; set; }
        public double MedianResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public double RequestsPerSecond => TotalDuration.TotalSeconds > 0 ? (SuccessfulRequests + FailedRequests) / TotalDuration.TotalSeconds : 0;
        public double SuccessRate => (SuccessfulRequests + FailedRequests) > 0 ? (double)SuccessfulRequests / (SuccessfulRequests + FailedRequests) : 0;
    }

    public class VerificationException : Exception
    {
        public VerificationException(string message) : base(message) { }
    }
}