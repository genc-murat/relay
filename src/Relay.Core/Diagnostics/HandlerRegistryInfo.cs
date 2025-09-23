using System;
using System.Collections.Generic;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Contains information about the current handler registry
/// </summary>
public class HandlerRegistryInfo
{
    /// <summary>
    /// Name of the assembly containing the handlers
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// When the registry information was generated
    /// </summary>
    public DateTime GenerationTime { get; set; }

    /// <summary>
    /// List of all registered handlers
    /// </summary>
    public List<HandlerInfo> Handlers { get; set; } = new();

    /// <summary>
    /// List of all registered pipeline behaviors
    /// </summary>
    public List<PipelineInfo> Pipelines { get; set; } = new();

    /// <summary>
    /// Any warnings or issues detected during registry analysis
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Total number of registered handlers
    /// </summary>
    public int TotalHandlers => Handlers.Count;

    /// <summary>
    /// Total number of registered pipelines
    /// </summary>
    public int TotalPipelines => Pipelines.Count;
}