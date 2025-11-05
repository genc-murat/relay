using Relay.Core.Caching.Attributes;

namespace Relay.Core.Tests.Caching.Behaviors;

/// <summary>
/// Test request and response classes for caching behavior tests
/// </summary>
public class TestRequest : Relay.Core.Contracts.Requests.IRequest<TestResponse> { }
public class TestResponse { }

[RelayCacheAttribute(KeyPattern = "test-fixed-key")]
public class CachedRequest : TestRequest { }

[RelayCacheAttribute(Enabled = false)]
public class DisabledCacheRequest : TestRequest { }

[RelayCacheAttribute(KeyPattern = "distributed-test-key", UseDistributedCache = true)]
public class DistributedCachedRequest : TestRequest { }

[RelayCacheAttribute(KeyPattern = "{RequestType}:{RequestHash}:{Region}")]
public class HashCachedRequest : TestRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[RelayCacheAttribute(KeyPattern = "tracked-test-key", Tags = new[] { "tag1", "tag2" })]
public class TrackedCachedRequest : TestRequest { }

[RelayCacheAttribute(KeyPattern = "sliding-expiry-key", SlidingExpirationSeconds = 300)]
public class SlidingExpiryRequest : TestRequest { }

[RelayCacheAttribute(KeyPattern = "absolute-expiry-key", AbsoluteExpirationSeconds = 600)]
public class AbsoluteExpiryRequest : TestRequest { }

