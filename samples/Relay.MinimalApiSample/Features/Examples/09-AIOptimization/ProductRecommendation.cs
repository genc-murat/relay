using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.AIOptimization;

/// <summary>
/// Request for AI-powered product recommendations
/// </summary>
public record GetProductRecommendationsRequest(
    Guid UserId,
    string Category,
    int Count = 10
) : IRequest<ProductRecommendationsResponse>;

/// <summary>
/// Response containing AI-powered product recommendations
/// </summary>
public record ProductRecommendationsResponse(
    IEnumerable<RecommendedProduct> Recommendations,
    AIOptimizationMetrics Metrics
);

/// <summary>
/// Recommended product with AI scoring
/// </summary>
public record RecommendedProduct(
    Guid ProductId,
    string Name,
    decimal Price,
    double RelevanceScore,
    string Reason
);

/// <summary>
/// AI optimization metrics for transparency
/// </summary>
public record AIOptimizationMetrics(
    string OptimizationStrategy,
    int ProcessingTimeMs,
    bool WasBatched,
    bool WasCached,
    double ConfidenceScore,
    string PerformanceGain
);
