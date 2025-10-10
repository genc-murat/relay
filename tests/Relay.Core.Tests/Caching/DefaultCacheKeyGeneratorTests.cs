using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using System;
using Xunit;

namespace Relay.Core.Tests.Caching
{
    public class DefaultCacheKeyGeneratorTests
    {
        private readonly DefaultCacheKeyGenerator _keyGenerator = new DefaultCacheKeyGenerator();

        public class SimpleRequest
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class AnotherRequest
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class ComplexRequest
        {
            public Guid RequestId { get; set; }
            public NestedData? Data { get; set; }
        }

        public class NestedData
        {
            public bool IsActive { get; set; }
            public string[]? Tags { get; set; }
        }

        [Fact]
        public void GenerateKey_WithUnifiedAttributeAndDefaultPattern_GeneratesCorrectKey()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new UnifiedCacheAttribute(); // Uses default pattern

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.StartsWith("SimpleRequest:", key);
            Assert.DoesNotContain("{", key); // No unresolved placeholders
            Assert.DoesNotContain("}", key);
        }

        [Fact]
        public void GenerateKey_WithUnifiedAttributeAndCustomPattern_GeneratesCorrectKey()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new UnifiedCacheAttribute
            {
                KeyPattern = "MyCache:{Region}:{RequestType}-{RequestHash}",
                Region = "Users"
            };

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.StartsWith("MyCache:Users:SimpleRequest-", key);
        }

        [Fact]
        public void GenerateKey_ForIdenticalRequests_GeneratesSameKey()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 123, Name = "SameName" };
            var request2 = new SimpleRequest { Id = 123, Name = "SameName" };
            var attribute = new UnifiedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void GenerateKey_ForDifferentRequests_GeneratesDifferentKeys()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 1, Name = "A" };
            var request2 = new SimpleRequest { Id = 2, Name = "B" };
            var attribute = new UnifiedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GenerateKey_ForDifferentRequestTypes_GeneratesDifferentKeys()
        {
            // Arrange
            var request1 = new SimpleRequest { Id = 1, Name = "Same" };
            var request2 = new AnotherRequest { Id = 1, Name = "Same" };
            var attribute = new UnifiedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            Assert.NotEqual(key1, key2);
            Assert.Contains("SimpleRequest", key1);
            Assert.Contains("AnotherRequest", key2);
        }

        [Fact]
        public void GenerateKey_WithComplexObject_GeneratesKey()
        {
            // Arrange
            var request = new ComplexRequest
            {
                RequestId = Guid.NewGuid(),
                Data = new NestedData
                {
                    IsActive = true,
                    Tags = new[] { "tag1", "tag2" }
                }
            };
            var attribute = new UnifiedCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("ComplexRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithDifferentPropertyOrder_GeneratesSameKey()
        {
            // Arrange
            var request1 = new SimpleRequest { Name = "OrderTest", Id = 99 };
            var request2 = new SimpleRequest { Id = 99, Name = "OrderTest" };
            var attribute = new UnifiedCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            Assert.Equal(key1, key2);
        }
    }
}
