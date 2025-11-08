namespace Relay.Core.Testing;

/// <summary>
/// <summary>
/// Represents expected call counts for mock verification.
/// </summary>
public class CallTimes
{
    private readonly int _from;
    private readonly int _to;

    private CallTimes(int from, int to)
    {
        _from = from;
        _to = to;
    }

    /// <summary>
    /// Gets a human-readable description of the expected call count range.
    /// </summary>
    /// <value>A string describing the expected call count.</value>
    public string Description => _from == _to ? $"exactly {_from}" : $"between {_from} and {_to}";

    /// <summary>
    /// Validates whether the actual call count falls within the expected range.
    /// </summary>
    /// <param name="actualCount">The actual number of calls made.</param>
    /// <returns><c>true</c> if the actual count is within the expected range; otherwise, <c>false</c>.</returns>
    public bool Validate(int actualCount)
    {
        return actualCount >= _from && actualCount <= _to;
    }

    /// <summary>
    /// Specifies that the method should be called exactly once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing exactly one call.</returns>
    public static CallTimes Once() => new(1, 1);

    /// <summary>
    /// Specifies that the method should never be called.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing zero calls.</returns>
    public static CallTimes Never() => new(0, 0);

    /// <summary>
    /// Specifies that the method should be called at least once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing one or more calls.</returns>
    public static CallTimes AtLeastOnce() => new(1, int.MaxValue);

    /// <summary>
    /// Specifies that the method should be called at most once.
    /// </summary>
    /// <returns>A <see cref="CallTimes"/> instance representing zero or one call.</returns>
    public static CallTimes AtMostOnce() => new(0, 1);

    /// <summary>
    /// Specifies that the method should be called exactly the specified number of times.
    /// </summary>
    /// <param name="count">The exact number of calls expected.</param>
    /// <returns>A <see cref="CallTimes"/> instance representing exactly <paramref name="count"/> calls.</returns>
    public static CallTimes Exactly(int count) => new(count, count);

    /// <summary>
    /// Specifies that the method should be called between the specified minimum and maximum number of times.
    /// </summary>
    /// <param name="from">The minimum number of calls expected (inclusive).</param>
    /// <param name="to">The maximum number of calls expected (inclusive).</param>
    /// <returns>A <see cref="CallTimes"/> instance representing calls between <paramref name="from"/> and <paramref name="to"/>.</returns>
    public static CallTimes Between(int from, int to) => new(from, to);
}
