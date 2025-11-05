using System;

namespace Relay.Core.Testing;

/// <summary>
/// Configuration settings for load testing scenarios.
/// </summary>
public class LoadTestConfiguration
{
    private int _totalRequests = 100;
    private int _maxConcurrency = 10;
    private int _rampUpDelayMs = 0;

    /// <summary>
    /// Gets or sets the total number of requests to execute during the load test.
    /// </summary>
    /// <value>The total number of requests. Must be greater than 0.</value>
    /// <exception cref="ArgumentException">Thrown when the value is less than or equal to 0.</exception>
    public int TotalRequests
    {
        get => _totalRequests;
        set => _totalRequests = value > 0 ? value : throw new ArgumentException("TotalRequests must be greater than 0");
    }

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests to execute.
    /// </summary>
    /// <value>The maximum concurrency level. Must be greater than 0.</value>
    /// <exception cref="ArgumentException">Thrown when the value is less than or equal to 0.</exception>
    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set => _maxConcurrency = value > 0 ? value : throw new ArgumentException("MaxConcurrency must be greater than 0");
    }

    /// <summary>
    /// Gets or sets the delay in milliseconds between starting each concurrent request during ramp-up.
    /// </summary>
    /// <value>The ramp-up delay in milliseconds. Cannot be negative.</value>
    /// <exception cref="ArgumentException">Thrown when the value is negative.</exception>
    public int RampUpDelayMs
    {
        get => _rampUpDelayMs;
        set => _rampUpDelayMs = value >= 0 ? value : throw new ArgumentException("RampUpDelayMs cannot be negative");
    }

    /// <summary>
    /// Gets or sets the duration of the load test.
    /// </summary>
    /// <value>The test duration. Defaults to 1 minute.</value>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the number of concurrent users to simulate.
    /// </summary>
    /// <value>The number of concurrent users. Defaults to 1.</value>
    public int ConcurrentUsers { get; set; } = 1;

    /// <summary>
    /// Gets or sets the time to ramp up to full concurrency.
    /// </summary>
    /// <value>The ramp-up time. Defaults to 0 (immediate full concurrency).</value>
    public TimeSpan RampUpTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the interval between requests for each virtual user.
    /// </summary>
    /// <value>The request interval. Defaults to 1 second.</value>
    public TimeSpan RequestInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets a value indicating whether to monitor memory usage during the test.
    /// </summary>
    /// <value><c>true</c> to monitor memory usage; otherwise, <c>false</c>. Defaults to <c>false</c>.</value>
    public bool MonitorMemoryUsage { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to collect detailed timing metrics.
    /// </summary>
    /// <value><c>true</c> to collect detailed timing; otherwise, <c>false</c>. Defaults to <c>true</c>.</value>
    public bool CollectDetailedTiming { get; set; } = true;

    /// <summary>
    /// Gets or sets the warm-up duration before collecting metrics.
    /// </summary>
    /// <value>The warm-up duration. Defaults to 10 seconds.</value>
    public TimeSpan WarmUpDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadTestConfiguration"/> class with default values.
    /// </summary>
    public LoadTestConfiguration() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadTestConfiguration"/> class with specified parameters.
    /// </summary>
    /// <param name="totalRequests">The total number of requests to execute.</param>
    /// <param name="maxConcurrency">The maximum number of concurrent requests.</param>
    /// <param name="rampUpDelayMs">The delay in milliseconds between starting concurrent requests.</param>
    public LoadTestConfiguration(int totalRequests, int maxConcurrency, int rampUpDelayMs = 0)
    {
        TotalRequests = totalRequests;
        MaxConcurrency = maxConcurrency;
        RampUpDelayMs = rampUpDelayMs;
    }
}