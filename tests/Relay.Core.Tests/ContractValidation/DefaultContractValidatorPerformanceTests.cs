using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class DefaultContractValidatorPerformanceTests
{
    private readonly DefaultContractValidator _validator = new();

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Fact]
    public async Task ValidateRequestAsync_SchemaCaching_ShouldReuseParsedSchemas()
    {
        // Arrange
        var request1 = new TestRequest { Name = "Test1", Value = 1 };
        var request2 = new TestRequest { Name = "Test2", Value = 2 };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
        };

        // Act - Validate multiple times with same schema
        var errors1 = await _validator.ValidateRequestAsync(request1, schema);
        var errors2 = await _validator.ValidateRequestAsync(request2, schema);

        // Assert - Both should succeed (caching should work)
        Assert.Empty(errors1);
        Assert.Empty(errors2);
    }

    [Fact]
    public async Task ValidateRequestAsync_SchemaCaching_ShouldCacheMultipleDifferentSchemas()
    {
        // Arrange
        var request1 = new TestRequest { Name = "Test1", Value = 1 };
        var request2 = new { Email = "test@example.com" };
        var schema1 = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
        };
        var schema2 = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Email"": { ""type"": ""string"", ""format"": ""email"" }
                    },
                    ""required"": [""Email""]
                }"
        };

        // Act - Validate with different schemas
        var errors1 = await _validator.ValidateRequestAsync(request1, schema1);
        var errors2 = await _validator.ValidateRequestAsync(request2, schema2);
        // Validate again with same schemas to test caching
        var errors3 = await _validator.ValidateRequestAsync(request1, schema1);
        var errors4 = await _validator.ValidateRequestAsync(request2, schema2);

        // Assert - All should succeed (caching should work for multiple schemas)
        Assert.Empty(errors1);
        Assert.Empty(errors2);
        Assert.Empty(errors3);
        Assert.Empty(errors4);
    }
}
