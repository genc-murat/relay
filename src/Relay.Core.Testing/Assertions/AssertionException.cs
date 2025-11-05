using System;

namespace Relay.Core.Testing;

/// <summary>
/// Exception thrown when an assertion fails.
/// </summary>
public class AssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AssertionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AssertionException(string message, Exception innerException) : base(message, innerException) { }
}