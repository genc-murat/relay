using System;

namespace Relay.Core.HandlerVersioning;

/// <summary>
/// Information about a handler version
/// </summary>
internal sealed class HandlerVersionInfo
{
    public Version Version { get; init; } = new(1, 0);
    public bool IsDeprecated { get; init; }
    public string? DeprecationMessage { get; init; }
    public Type HandlerType { get; init; } = typeof(object);
}
