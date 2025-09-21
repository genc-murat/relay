using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Default implementation of request tracing using AsyncLocal for context tracking
/// </summary>
public class RequestTracer : IRequestTracer
{
    private readonly AsyncLocal<RequestTrace?> _currentTrace = new();
    private readonly ConcurrentQueue<RequestTrace> _completedTraces = new();
    private readonly object _lockObject = new();
    private int _maxCompletedTraces = 1000;
    private int _activeTraceCount = 0;
    
    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;
    
    /// <inheritdoc />
    public int ActiveTraceCount => _activeTraceCount;
    
    /// <inheritdoc />
    public int CompletedTraceCount => _completedTraces.Count;
    
    /// <summary>
    /// Maximum number of completed traces to retain in memory
    /// </summary>
    public int MaxCompletedTraces
    {
        get => _maxCompletedTraces;
        set => _maxCompletedTraces = Math.Max(1, value);
    }
    
    /// <inheritdoc />
    public RequestTrace StartTrace<TRequest>(TRequest request, string? correlationId = null)
    {
        if (!IsEnabled)
        {
            return CreateEmptyTrace<TRequest>();
        }
        
        var trace = new RequestTrace
        {
            RequestId = Guid.NewGuid(),
            RequestType = typeof(TRequest),
            StartTime = DateTimeOffset.UtcNow,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N").Substring(0, 8),
            Steps = new List<TraceStep>()
        };
        
        // Add request metadata if available
        if (request != null)
        {
            trace.Metadata["RequestData"] = request.ToString();
            trace.Metadata["RequestTypeName"] = typeof(TRequest).Name;
        }
        
        _currentTrace.Value = trace;
        Interlocked.Increment(ref _activeTraceCount);
        
        return trace;
    }
    
    /// <inheritdoc />
    public RequestTrace? GetCurrentTrace()
    {
        return IsEnabled ? _currentTrace.Value : null;
    }
    
    /// <inheritdoc />
    public void AddStep(string stepName, TimeSpan duration, string category = "Unknown", object? metadata = null)
    {
        if (!IsEnabled)
            return;
            
        var trace = _currentTrace.Value;
        if (trace == null)
            return;
        
        var step = new TraceStep
        {
            Name = stepName,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = duration,
            Category = category,
            Metadata = metadata
        };
        
        lock (trace.Steps)
        {
            trace.Steps.Add(step);
        }
    }
    
    /// <inheritdoc />
    public void AddHandlerStep(string stepName, TimeSpan duration, Type handlerType, string category = "Handler", object? metadata = null)
    {
        if (!IsEnabled)
            return;
            
        var trace = _currentTrace.Value;
        if (trace == null)
            return;
        
        var step = new TraceStep
        {
            Name = stepName,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = duration,
            Category = category,
            HandlerType = handlerType.Name,
            Metadata = metadata
        };
        
        lock (trace.Steps)
        {
            trace.Steps.Add(step);
        }
    }
    
    /// <inheritdoc />
    public void RecordException(Exception exception, string? stepName = null)
    {
        if (!IsEnabled)
            return;
            
        var trace = _currentTrace.Value;
        if (trace == null)
            return;
        
        // Set exception on the trace
        trace.Exception = exception;
        
        // Add an exception step if a step name is provided
        if (!string.IsNullOrEmpty(stepName))
        {
            var step = new TraceStep
            {
                Name = stepName,
                Timestamp = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                Category = "Exception",
                Exception = exception,
                Metadata = new { ExceptionType = exception.GetType().Name, Message = exception.Message }
            };
            
            lock (trace.Steps)
            {
                trace.Steps.Add(step);
            }
        }
    }
    
    /// <inheritdoc />
    public void CompleteTrace(bool success = true)
    {
        if (!IsEnabled)
            return;
            
        var trace = _currentTrace.Value;
        if (trace == null)
            return;
        
        trace.EndTime = DateTimeOffset.UtcNow;
        
        // If not successful but no exception was recorded, mark it as failed
        if (!success && trace.Exception == null)
        {
            trace.Metadata["CompletedSuccessfully"] = false;
        }
        
        // Move to completed traces
        _completedTraces.Enqueue(trace);
        
        // Trim completed traces if we exceed the limit
        TrimCompletedTraces();
        
        // Clear current trace and decrement active count
        _currentTrace.Value = null;
        Interlocked.Decrement(ref _activeTraceCount);
    }
    
    /// <inheritdoc />
    public IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null)
    {
        if (!IsEnabled)
            return Enumerable.Empty<RequestTrace>();
        
        var traces = _completedTraces.ToArray();
        
        if (since.HasValue)
        {
            traces = traces.Where(t => t.StartTime >= since.Value).ToArray();
        }
        
        return traces.OrderByDescending(t => t.StartTime);
    }
    
    /// <inheritdoc />
    public void ClearTraces()
    {
        lock (_lockObject)
        {
            while (_completedTraces.TryDequeue(out _))
            {
                // Clear all completed traces
            }
        }
    }
    
    private void TrimCompletedTraces()
    {
        lock (_lockObject)
        {
            while (_completedTraces.Count > _maxCompletedTraces)
            {
                _completedTraces.TryDequeue(out _);
            }
        }
    }
    
    private static RequestTrace CreateEmptyTrace<TRequest>()
    {
        return new RequestTrace
        {
            RequestId = Guid.Empty,
            RequestType = typeof(TRequest),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            Steps = new List<TraceStep>()
        };
    }
}