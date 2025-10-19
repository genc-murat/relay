using System;

namespace Relay.Core.HandlerVersioning;

/// <summary>
/// Exception thrown when a handler version is not found
/// </summary>
public sealed class HandlerVersionNotFoundException : Exception
{
    public Type RequestType { get; }
    public Version? RequestedVersion { get; }
    public Version? MinVersion { get; }
    public Version? MaxVersion { get; }

    public HandlerVersionNotFoundException(Type requestType, Version requestedVersion)
        : base("Handler version {requestedVersion} not found for request type {requestType.Name}")
    {
        RequestType = requestType;
        RequestedVersion = requestedVersion;
    }

    public HandlerVersionNotFoundException(Type requestType, Version? minVersion, Version? maxVersion)
        : base("No compatible handler version found for request type {requestType.Name} (min: {minVersion}, max: {maxVersion})")
    {
        RequestType = requestType;
        MinVersion = minVersion;
        MaxVersion = maxVersion;
    }
}
