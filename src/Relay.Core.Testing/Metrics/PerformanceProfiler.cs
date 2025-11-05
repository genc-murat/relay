using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Main class for performance profiling with session management and metrics collection.
/// </summary>
public class PerformanceProfiler
{
    private readonly MetricsCollector _collector = new();
    private readonly Dictionary<string, ProfileSession> _sessions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the active profiling session, or null if none is active.
    /// </summary>
    public ProfileSession? ActiveSession { get; private set; }

    /// <summary>
    /// Gets all profiling sessions.
    /// </summary>
    public IReadOnlyDictionary<string, ProfileSession> Sessions
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, ProfileSession>(_sessions);
            }
        }
    }

    /// <summary>
    /// Starts a new profiling session with the specified name.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>The started profile session.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a session with the same name already exists.</exception>
    public ProfileSession StartSession(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName))
            throw new ArgumentException("Session name cannot be null or empty.", nameof(sessionName));

        lock (_lock)
        {
            if (_sessions.ContainsKey(sessionName))
                throw new InvalidOperationException($"A session with name '{sessionName}' already exists.");

            var session = new ProfileSession(sessionName);
            session.Start();
            _sessions[sessionName] = session;
            ActiveSession = session;

            return session;
        }
    }

    /// <summary>
    /// Stops the specified profiling session.
    /// </summary>
    /// <param name="sessionName">The name of the session to stop.</param>
    /// <exception cref="InvalidOperationException">Thrown if the session does not exist or is not running.</exception>
    public void StopSession(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName))
            throw new ArgumentException("Session name cannot be null or empty.", nameof(sessionName));

        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionName, out var session))
                throw new InvalidOperationException($"Session '{sessionName}' does not exist.");

            session.Stop();

            if (ActiveSession == session)
                ActiveSession = null;
        }
    }

    /// <summary>
    /// Stops the currently active session.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public void StopActiveSession()
    {
        if (ActiveSession == null)
            throw new InvalidOperationException("No active session to stop.");

        StopSession(ActiveSession.SessionName);
    }

    /// <summary>
    /// Profiles an operation within the active session.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The operation to profile.</param>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public void Profile(string operationName, Action operation)
    {
        if (ActiveSession == null)
            throw new ProfilerNotStartedException("No active profiling session. Start a session before profiling operations.");

        var metrics = _collector.Collect(operationName, operation);
        ActiveSession.AddOperation(metrics);
    }

    /// <summary>
    /// Profiles an asynchronous operation within the active session.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The asynchronous operation to profile.</param>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public async Task ProfileAsync(string operationName, Func<Task> operation)
    {
        if (ActiveSession == null)
            throw new ProfilerNotStartedException("No active profiling session. Start a session before profiling operations.");

        var metrics = await _collector.CollectAsync(operationName, operation);
        ActiveSession.AddOperation(metrics);
    }

    /// <summary>
    /// Profiles an operation that returns a value within the active session.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The operation to profile.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public T Profile<T>(string operationName, Func<T> operation)
    {
        if (ActiveSession == null)
            throw new ProfilerNotStartedException("No active profiling session. Start a session before profiling operations.");

        var (result, metrics) = _collector.Collect(operationName, operation);
        ActiveSession.AddOperation(metrics);
        return result;
    }

    /// <summary>
    /// Profiles an asynchronous operation that returns a value within the active session.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The asynchronous operation to profile.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)
    {
        if (ActiveSession == null)
            throw new ProfilerNotStartedException("No active profiling session. Start a session before profiling operations.");

        var (result, metrics) = await _collector.CollectAsync(operationName, operation);
        ActiveSession.AddOperation(metrics);
        return result;
    }

    /// <summary>
    /// Gets a profiling session by name.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <returns>The profile session, or null if not found.</returns>
    public ProfileSession? GetSession(string sessionName)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(sessionName, out var session);
            return session;
        }
    }

    /// <summary>
    /// Removes a profiling session.
    /// </summary>
    /// <param name="sessionName">The name of the session to remove.</param>
    /// <returns>true if the session was removed; otherwise, false.</returns>
    public bool RemoveSession(string sessionName)
    {
        lock (_lock)
        {
            if (_sessions.Remove(sessionName, out var session) && ActiveSession == session)
            {
                ActiveSession = null;
            }
            return session != null;
        }
    }

    /// <summary>
    /// Clears all profiling sessions.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _sessions.Clear();
            ActiveSession = null;
        }
    }

    /// <summary>
    /// Generates a report for the specified session.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <param name="thresholds">Optional performance thresholds.</param>
    /// <returns>The profile report.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the session does not exist.</exception>
    public ProfileReport GenerateReport(string sessionName, PerformanceThresholds? thresholds = null)
    {
        var session = GetSession(sessionName);
        if (session == null)
            throw new InvalidOperationException($"Session '{sessionName}' does not exist.");

        return new ProfileReport(session, thresholds);
    }

    /// <summary>
    /// Generates a report for the active session.
    /// </summary>
    /// <param name="thresholds">Optional performance thresholds.</param>
    /// <returns>The profile report.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no session is active.</exception>
    public ProfileReport GenerateActiveReport(PerformanceThresholds? thresholds = null)
    {
        if (ActiveSession == null)
            throw new InvalidOperationException("No active session to report on.");

        return new ProfileReport(ActiveSession, thresholds);
    }
}