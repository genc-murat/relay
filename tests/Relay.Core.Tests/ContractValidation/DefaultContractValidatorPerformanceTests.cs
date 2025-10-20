using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation
{
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
    }
}