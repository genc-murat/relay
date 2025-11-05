using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using System;
using Xunit;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

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
            var attribute = new RelayCacheAttribute(); // Uses default pattern

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
            var attribute = new RelayCacheAttribute
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
            var attribute = new RelayCacheAttribute();

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
            var attribute = new RelayCacheAttribute();

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
            var attribute = new RelayCacheAttribute();

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
            var attribute = new RelayCacheAttribute();

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
            var attribute = new RelayCacheAttribute();

            // Act
            var key1 = _keyGenerator.GenerateKey(request1, attribute);
            var key2 = _keyGenerator.GenerateKey(request2, attribute);

            // Assert
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void GenerateKey_WithNullProperties_HandlesNullValues()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = null };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("SimpleRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithEmptyStringProperties_HandlesEmptyStrings()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "" };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("SimpleRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithSpecialCharactersInProperties_HandlesSpecialChars()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test@#$%^&*()" };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("SimpleRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithUnicodeCharacters_HandlesUnicode()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "测试数据🚀" };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("SimpleRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithVeryLongPropertyValues_HandlesLongStrings()
        {
            // Arrange
            var longName = new string('A', 10000);
            var request = new SimpleRequest { Id = 1, Name = longName };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("SimpleRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithCustomPatternAndMissingPlaceholders_LeavesUnresolvedPlaceholders()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new RelayCacheAttribute
            {
                KeyPattern = "Cache:{Region}:{RequestType}:{MissingPlaceholder}",
                Region = "Users"
            };

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.Equal("Cache:Users:SimpleRequest:{MissingPlaceholder}", key);
        }

        [Fact]
        public void GenerateKey_WithRegionContainingSpecialChars_HandlesSpecialCharsInRegion()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new RelayCacheAttribute
            {
                KeyPattern = "MyCache:{Region}:{RequestType}-{RequestHash}",
                Region = "User@Domain#123"
            };

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.StartsWith("MyCache:User@Domain#123:SimpleRequest-", key);
        }

        [Fact]
        public void GenerateKey_WithComplexObjectAndNullNestedProperties_HandlesNulls()
        {
            // Arrange
            var request = new ComplexRequest
            {
                RequestId = Guid.NewGuid(),
                Data = new NestedData
                {
                    IsActive = true,
                    Tags = null // null array
                }
            };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("ComplexRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithComplexObjectAndEmptyNestedArray_HandlesEmptyArrays()
        {
            // Arrange
            var request = new ComplexRequest
            {
                RequestId = Guid.NewGuid(),
                Data = new NestedData
                {
                    IsActive = false,
                    Tags = new string[0] // empty array
                }
            };
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("ComplexRequest:", key);
        }

        [Fact]
        public void GenerateKey_WithDefaultPatternAndNullRequestType_HandlesNullGracefully()
        {
            // Arrange
            SimpleRequest? request = null;
            var attribute = new RelayCacheAttribute();

            // Act
            var key = _keyGenerator.GenerateKey(request!, attribute);

            // Assert
            Assert.False(string.IsNullOrEmpty(key));
            Assert.Contains("SimpleRequest", key); // Uses the type name
        }

        [Fact]
        public void GenerateKey_WithCustomPatternAndNullRegion_UsesEmptyRegion()
        {
            // Arrange
            var request = new SimpleRequest { Id = 1, Name = "Test" };
            var attribute = new RelayCacheAttribute
            {
                KeyPattern = "Cache:{Region}:{RequestType}",
                Region = null
            };

            // Act
            var key = _keyGenerator.GenerateKey(request, attribute);

            // Assert
            Assert.StartsWith("Cache::SimpleRequest", key);
        }

        [Fact]
        public void GenerateKey_WithMultipleRequestsOfSameType_GeneratesUniqueKeys()
        {
            // Arrange
            var requests = new[]
            {
                new SimpleRequest { Id = 1, Name = "A" },
                new SimpleRequest { Id = 2, Name = "B" },
                new SimpleRequest { Id = 3, Name = "C" }
            };
            var attribute = new RelayCacheAttribute();
            var keys = new System.Collections.Generic.HashSet<string>();

            // Act
            foreach (var request in requests)
            {
                keys.Add(_keyGenerator.GenerateKey(request, attribute));
            }

            // Assert
            Assert.Equal(3, keys.Count);
        }
    }
}

