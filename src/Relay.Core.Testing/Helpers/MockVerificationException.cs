using System;

namespace Relay.Core.Testing;

/// <summary>
/// Exception thrown when mock verification fails.
/// </summary>
public class MockVerificationException : Exception
{
    public MockVerificationException(string message) : base(message) { }
}