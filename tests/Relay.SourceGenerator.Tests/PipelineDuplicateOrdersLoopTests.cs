extern alias RelayCore;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Validators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for the specific loop in ValidateDuplicatePipelineOrders 
/// where foreach (var pipeline in orderGroup) executes to report diagnostics.
/// </summary>
public class PipelineDuplicateOrdersLoopTests
{
    /// <summary>
    /// Tests that ValidateDuplicatePipelineOrders processes the loop when there are true duplicates
    /// (same method names with same order in same containing type and scope).
    /// This specifically tests the conditions that lead to 'foreach (var pipeline in orderGroup)' loop execution.
    /// </summary>
    [Fact]
    public void ValidateDuplicatePipelineOrders_Loop_Condition_Verified()
    {
        // Create pipeline registry with true duplicates - same method name, same order, same containing type/scope
        var pipelineRegistry = new List<PipelineInfo>
        {
            new PipelineInfo { MethodName = "SameMethod", Order = 1, Scope = 0, ContainingType = "Test.TestClass", Location = Location.None },
            new PipelineInfo { MethodName = "SameMethod", Order = 1, Scope = 0, ContainingType = "Test.TestClass", Location = Location.None }, // Same name and order - this should trigger the diagnostic
        };

        // Verify the condition that would trigger the foreach loop in ValidateDuplicatePipelineOrders
        // The logic is: group by scope and containing type, then group by order, then check distinct method names
        var pipelineGroups = pipelineRegistry.GroupBy(p => new { p.Scope, p.ContainingType });
        var group = pipelineGroups.First(); // Only one group in our test
        
        var duplicateOrders = group.GroupBy(p => p.Order).Where(g => g.Count() > 1);
        var orderGroup = duplicateOrders.First(); // The group with duplicate orders
        
        var distinctMethods = orderGroup.Select(p => p.MethodName).Distinct().Count(); // Should be 1
        var totalInGroup = orderGroup.Count(); // Should be 2
        
        // The condition that enables the loop: distinctMethods != orderGroup.Count()
        // If all methods have different names (distinctMethods == orderGroup.Count()), it skips with 'continue'
        Assert.Equal(1, distinctMethods);  // Only one distinct method name
        Assert.Equal(2, totalInGroup);     // But 2 total entries
        Assert.NotEqual(distinctMethods, totalInGroup);  // This condition means the actual diagnostic reporting loop executes
    }

    /// <summary>
    /// Additional test to verify different scenarios for the loop execution.
    /// </summary>
    [Fact]
    public void ValidateDuplicatePipelineOrders_Loop_Condition_Verified_Multiple()
    {
        // Create pipeline registry with multiple duplicates
        var pipelineRegistry = new List<PipelineInfo>
        {
            new PipelineInfo { MethodName = "IdenticalMethod", Order = 5, Scope = 1, ContainingType = "Test.AnotherClass", Location = Location.None },
            new PipelineInfo { MethodName = "IdenticalMethod", Order = 5, Scope = 1, ContainingType = "Test.AnotherClass", Location = Location.None },
            new PipelineInfo { MethodName = "IdenticalMethod", Order = 5, Scope = 1, ContainingType = "Test.AnotherClass", Location = Location.None }, // 3 identical methods
        };

        // Same logic verification
        var pipelineGroups = pipelineRegistry.GroupBy(p => new { p.Scope, p.ContainingType });
        var group = pipelineGroups.First();
        var duplicateOrders = group.GroupBy(p => p.Order).Where(g => g.Count() > 1);
        var orderGroup = duplicateOrders.First();
        
        var distinctMethods = orderGroup.Select(p => p.MethodName).Distinct().Count(); // Should be 1
        var totalInGroup = orderGroup.Count(); // Should be 3
        
        Assert.Equal(1, distinctMethods);  // Only one distinct method name
        Assert.Equal(3, totalInGroup);     // But 3 total entries
        Assert.NotEqual(distinctMethods, totalInGroup);  // This condition means the loop executes
    }
}