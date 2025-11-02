using System.Text;

namespace Relay.SourceGenerator.Generators;

/// <summary>
/// Strategy interface for code generation.
/// Follows the Strategy Pattern and Open/Closed Principle.
/// New generation strategies can be added without modifying existing code.
/// </summary>
public interface ICodeGenerationStrategy
{
    /// <summary>
    /// Gets the name of this generation strategy.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Determines if this strategy can be applied to the given context.
    /// </summary>
    /// <param name="context">The generation context</param>
    /// <returns>True if the strategy can be applied, false otherwise</returns>
    bool CanApply(ICodeGenerationContext context);

    /// <summary>
    /// Applies the generation strategy to the given context.
    /// </summary>
    /// <param name="context">The generation context</param>
    /// <param name="builder">The string builder to append generated code to</param>
    void Apply(ICodeGenerationContext context, StringBuilder builder);
}
