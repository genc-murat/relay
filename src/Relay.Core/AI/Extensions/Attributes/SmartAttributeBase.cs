using System;

namespace Relay.Core.AI;

/// <summary>
/// Base class for all Smart AI optimization attributes.
/// Provides common functionality and shared properties.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public abstract class SmartAttributeBase : Attribute
{
    /// <summary>
    /// Gets or sets whether to use AI-predicted optimal settings.
    /// </summary>
    public bool UseAIPrediction { get; set; } = true;

    /// <summary>
    /// Gets or sets the optimization priority.
    /// </summary>
    public OptimizationPriority Priority { get; set; } = OptimizationPriority.Medium;

    /// <summary>
    /// Gets or sets whether to enable detailed monitoring for this optimization.
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the monitoring level for this optimization.
    /// </summary>
    public MonitoringLevel MonitoringLevel { get; set; } = MonitoringLevel.Standard;
}