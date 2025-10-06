using System;

namespace Relay.Core.Diagnostics.Services;

/// <summary>
/// Response wrapper for diagnostic operations
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class DiagnosticResponse<T>
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code equivalent
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The response data (if successful)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static DiagnosticResponse<T> Success(T data, int statusCode = 200)
    {
        return new DiagnosticResponse<T>
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static DiagnosticResponse<T> Error(string message, Exception? exception = null, int statusCode = 500)
    {
        return new DiagnosticResponse<T>
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = message,
            ErrorDetails = exception != null ? new
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            } : null
        };
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    public static DiagnosticResponse<T> NotFound(string message = "Resource not found")
    {
        return new DiagnosticResponse<T>
        {
            IsSuccess = false,
            StatusCode = 404,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// Creates a bad request response
    /// </summary>
    public static DiagnosticResponse<T> BadRequest(string message)
    {
        return new DiagnosticResponse<T>
        {
            IsSuccess = false,
            StatusCode = 400,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// Creates a service unavailable response
    /// </summary>
    public static DiagnosticResponse<T> ServiceUnavailable(string message)
    {
        return new DiagnosticResponse<T>
        {
            IsSuccess = false,
            StatusCode = 503,
            ErrorMessage = message
        };
    }
}

/// <summary>
/// Non-generic diagnostic response for operations that don't return data
/// </summary>
public class DiagnosticResponse : DiagnosticResponse<object>
{
    /// <summary>
    /// Creates a successful response with no data
    /// </summary>
    public static DiagnosticResponse Success(int statusCode = 200)
    {
        return new DiagnosticResponse
        {
            IsSuccess = true,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates a successful response with a message
    /// </summary>
    public static DiagnosticResponse Success(string message, int statusCode = 200)
    {
        return new DiagnosticResponse
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = new { message }
        };
    }
}