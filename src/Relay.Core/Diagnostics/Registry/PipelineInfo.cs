using System;

namespace Relay.Core.Diagnostics.Registry;

/// <summary>
/// Information about a registered pipeline behavior
/// </summary>
public class PipelineInfo : IEquatable<PipelineInfo>
{
    /// <summary>
    /// The type implementing the pipeline behavior
    /// </summary>
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>
    /// The method name implementing the pipeline
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Execution order of the pipeline
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Scope of the pipeline (Global, Request, etc.)
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pipeline is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PipelineInfo);
    }

    /// <summary>
    /// Determines whether the specified PipelineInfo is equal to the current PipelineInfo.
    /// </summary>
    public bool Equals(PipelineInfo? other)
    {
        if (other is null)
            return false;

        return PipelineType == other.PipelineType &&
               MethodName == other.MethodName &&
               Order == other.Order &&
               Scope == other.Scope &&
               IsEnabled == other.IsEnabled;
    }

    /// <summary>
    /// Returns a hash code for the current object.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(PipelineType, MethodName, Order, Scope, IsEnabled);
    }
}