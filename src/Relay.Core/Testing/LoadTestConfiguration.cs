using System;

namespace Relay.Core.Testing;

public class LoadTestConfiguration
{
    private int _totalRequests = 100;
    private int _maxConcurrency = 10;
    private int _rampUpDelayMs = 0;

    public int TotalRequests
    {
        get => _totalRequests;
        set => _totalRequests = value > 0 ? value : throw new ArgumentException("TotalRequests must be greater than 0");
    }

    public int MaxConcurrency
    {
        get => _maxConcurrency;
        set => _maxConcurrency = value > 0 ? value : throw new ArgumentException("MaxConcurrency must be greater than 0");
    }

    public int RampUpDelayMs
    {
        get => _rampUpDelayMs;
        set => _rampUpDelayMs = value >= 0 ? value : throw new ArgumentException("RampUpDelayMs cannot be negative");
    }

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

    public LoadTestConfiguration() { }

    public LoadTestConfiguration(int totalRequests, int maxConcurrency, int rampUpDelayMs = 0)
    {
        TotalRequests = totalRequests;
        MaxConcurrency = maxConcurrency;
        RampUpDelayMs = rampUpDelayMs;
    }
}