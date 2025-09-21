using System;
using FluentAssertions;
using FluentAssertions.Types;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// FluentAssertions extensions for testing
/// </summary>
public static class FluentAssertionsExtensions
{
    /// <summary>
    /// Asserts that a type is an interface
    /// </summary>
    public static AndConstraint<TypeAssertions> BeInterface(this TypeAssertions assertions, string because = "", params object[] becauseArgs)
    {
        assertions.Subject.Should().NotBeNull(because, becauseArgs);
        assertions.Subject!.IsInterface.Should().BeTrue(because, becauseArgs);
        return new AndConstraint<TypeAssertions>(assertions);
    }
}