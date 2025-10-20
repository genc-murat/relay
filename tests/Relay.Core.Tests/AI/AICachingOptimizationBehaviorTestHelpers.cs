using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Tests.AI
{
    // Test classes for AICachingOptimizationBehavior tests
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(UseDynamicTtl = true, MinAccessFrequency = 1, MinPredictedHitRate = 0.0)]
    public class TestIntelligentCachingRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(EnableAIAnalysis = false)]
    public class TestIntelligentCachingDisabledRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(MinAccessFrequency = 10)]
    public class TestHighMinAccessFrequencyRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(MinPredictedHitRate = 0.8, MinAccessFrequency = 1)]
    public class TestHighMinHitRateRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(UseDynamicTtl = true, MinAccessFrequency = 0)]
    public class TestDynamicTtlRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(PreferredScope = CacheScope.User, MinAccessFrequency = 0)]
    public class TestUserScopeRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(PreferredScope = CacheScope.Session, MinAccessFrequency = 0)]
    public class TestSessionScopeRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [IntelligentCaching(PreferredScope = CacheScope.Request, MinAccessFrequency = 0)]
    public class TestRequestScopeRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestNonSerializableRequest : IRequest<TestResponse>
    {
        // This will cause JSON serialization to potentially fail in some scenarios
        public object NonSerializableValue { get; set; } = new object();
    }

    public class TestLargeRequest : IRequest<TestLargeResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestSizedRequest : IRequest<TestSizedResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestSizedResponse : IEstimateSize
    {
        public string Result { get; set; } = string.Empty;
        public long Size { get; set; }

        public long EstimateSize() => Size;
    }

    public class TestLargeResponse : IEstimateSize
    {
        public string Result { get; set; } = string.Empty;

        public long EstimateSize() => 2 * 1024 * 1024; // 2MB, larger than default 1MB limit
    }
}