using System;
using System.Threading.Tasks;

namespace Relay.Core;

/// <summary>
/// Represents a void return type for requests that don't return a value
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Gets the singleton instance of Unit
    /// </summary>
    public static Unit Value => default;

    /// <summary>
    /// Gets a completed ValueTask with Unit value
    /// </summary>
    public static ValueTask<Unit> Task => ValueTask.FromResult(Value);

    /// <summary>
    /// Determines whether the specified Unit is equal to the current Unit
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether the specified object is equal to the current Unit
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this Unit
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns a string representation of the Unit
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether two Unit instances are equal
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two Unit instances are not equal
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;

    /// <summary>
    /// Implicit conversion from any value to Unit
    /// </summary>
    public static implicit operator Unit(ValueTuple _) => default;

    /// <summary>
    /// Implicit conversion from Unit to ValueTask<Unit>
    /// </summary>
    public static implicit operator ValueTask<Unit>(Unit _) => Task;
}