using System;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Configuration;

/// <summary>
/// Configuration for Relay source generator with value-based equality for incremental caching.
/// </summary>
public sealed class RelayConfiguration : IEquatable<RelayConfiguration>
{
    /// <summary>
    /// Gets the generation options.
    /// </summary>
    public GenerationOptions Options { get; set; } = GenerationOptions.Default;

    /// <summary>
    /// Gets the timestamp when configuration was created (for cache invalidation).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Default configuration instance.
    /// </summary>
    public static RelayConfiguration Default => new RelayConfiguration();

    public bool Equals(RelayConfiguration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        var options = Options;
        var otherOptions = other.Options;

        return options.IncludeDebugInfo == otherOptions.IncludeDebugInfo &&
               options.IncludeDocumentation == otherOptions.IncludeDocumentation &&
               options.EnableNullableContext == otherOptions.EnableNullableContext &&
               options.UseAggressiveInlining == otherOptions.UseAggressiveInlining &&
               options.CustomNamespace == otherOptions.CustomNamespace &&
               options.AssemblyName == otherOptions.AssemblyName &&
               options.EnablePerformanceOptimizations == otherOptions.EnablePerformanceOptimizations &&
               options.MaxDegreeOfParallelism == otherOptions.MaxDegreeOfParallelism &&
               options.EnableKeyedServices == otherOptions.EnableKeyedServices &&
               options.EnableAggressiveInlining == otherOptions.EnableAggressiveInlining &&
               options.EnableDIGeneration == otherOptions.EnableDIGeneration &&
               options.EnableHandlerRegistry == otherOptions.EnableHandlerRegistry &&
               options.EnableOptimizedDispatcher == otherOptions.EnableOptimizedDispatcher &&
               options.EnableNotificationDispatcher == otherOptions.EnableNotificationDispatcher &&
               options.EnablePipelineRegistry == otherOptions.EnablePipelineRegistry &&
               options.EnableEndpointMetadata == otherOptions.EnableEndpointMetadata;
    }

    public override bool Equals(object? obj)
    {
        return obj is RelayConfiguration other && Equals(other);
    }

    public override int GetHashCode()
    {
        var options = Options;
        
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + options.IncludeDebugInfo.GetHashCode();
            hash = hash * 31 + options.IncludeDocumentation.GetHashCode();
            hash = hash * 31 + options.EnableNullableContext.GetHashCode();
            hash = hash * 31 + options.UseAggressiveInlining.GetHashCode();
            hash = hash * 31 + (options.CustomNamespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (options.AssemblyName?.GetHashCode() ?? 0);
            hash = hash * 31 + options.EnablePerformanceOptimizations.GetHashCode();
            hash = hash * 31 + options.MaxDegreeOfParallelism.GetHashCode();
            hash = hash * 31 + options.EnableKeyedServices.GetHashCode();
            hash = hash * 31 + options.EnableAggressiveInlining.GetHashCode();
            hash = hash * 31 + options.EnableDIGeneration.GetHashCode();
            hash = hash * 31 + options.EnableHandlerRegistry.GetHashCode();
            hash = hash * 31 + options.EnableOptimizedDispatcher.GetHashCode();
            hash = hash * 31 + options.EnableNotificationDispatcher.GetHashCode();
            hash = hash * 31 + options.EnablePipelineRegistry.GetHashCode();
            hash = hash * 31 + options.EnableEndpointMetadata.GetHashCode();
            return hash;
        }
    }
}
