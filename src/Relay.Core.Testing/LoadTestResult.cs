using System;
using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Contains the results of a load test execution.
/// </summary>
public class LoadTestResult
{
    /// <summary>
    /// Gets or sets the type of request that was tested.
    /// </summary>
    /// <value>The request type name. Defaults to an empty string.</value>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the load test started.
    /// </summary>
    /// <value>The start timestamp.</value>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the load test completed.
    /// </summary>
    /// <value>The completion timestamp, or <c>null</c> if the test is still running.</value>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the load test execution.
    /// </summary>
    /// <value>The total duration.</value>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the configuration used for the load test.
    /// </summary>
    /// <value>The load test configuration.</value>
    public LoadTestConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    /// <value>The count of successful requests.</value>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    /// <value>The count of failed requests.</value>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the list of individual response times in milliseconds.
    /// </summary>
    /// <value>The collection of response times.</value>
    public List<double> ResponseTimes { get; set; } = new();

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    /// <value>The average response time.</value>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the median response time in milliseconds.
    /// </summary>
    /// <value>The median response time.</value>
    public double MedianResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile response time in milliseconds.
    /// </summary>
    /// <value>The P95 response time.</value>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile response time in milliseconds.
    /// </summary>
    /// <value>The P99 response time.</value>
    public double P99ResponseTime { get; set; }

    /// <summary>
    /// Gets the throughput in requests per second.
    /// </summary>
    /// <value>The requests per second rate.</value>
    public double RequestsPerSecond => TotalDuration.TotalSeconds > 0 ? (SuccessfulRequests + FailedRequests) / TotalDuration.TotalSeconds : 0;

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    /// <value>The success rate.</value>
    public double SuccessRate => (SuccessfulRequests + FailedRequests) > 0 ? (double)SuccessfulRequests / (SuccessfulRequests + FailedRequests) : 0;

    /// <summary>
    /// Gets the total number of requests executed.
    /// </summary>
    /// <value>The total request count.</value>
    public int TotalRequests => SuccessfulRequests + FailedRequests;

    /// <summary>
    /// Gets the error rate as a percentage (0.0 to 1.0).
    /// </summary>
    /// <value>The error rate.</value>
    public double ErrorRate => (SuccessfulRequests + FailedRequests) > 0 ? 1.0 - SuccessRate : 0;

    /// <summary>
    /// Gets or sets the peak memory usage in bytes during the test.
    /// </summary>
    /// <value>The peak memory usage, or 0 if memory monitoring was not enabled.</value>
    public long PeakMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the average memory usage in bytes during the test.
    /// </summary>
    /// <value>The average memory usage, or 0 if memory monitoring was not enabled.</value>
    public long AverageMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a memory leak was detected.
    /// </summary>
    /// <value><c>true</c> if a memory leak was detected; otherwise, <c>false</c>.</value>
    public bool MemoryLeakDetected { get; set; }

    /// <summary>
    /// Gets or sets the throughput in requests per second (alias for RequestsPerSecond).
    /// </summary>
    /// <value>The throughput.</value>
    public double Throughput => RequestsPerSecond;
}