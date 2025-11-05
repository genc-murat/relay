using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class JsonSchemaContractTests
    {
        [Fact]
        public void JsonSchemaContract_ShouldInitializeWithDefaults()
        {
            // Act
            var contract = new JsonSchemaContract();

            // Assert
            Assert.Empty(contract.Schema);
            Assert.Equal("application/json", contract.ContentType);
            Assert.Equal("http://json-schema.org/draft-07/schema#", contract.SchemaVersion);
            Assert.NotNull(contract.Properties);
            Assert.Empty(contract.Properties);
        }

        [Fact]
        public void JsonSchemaContract_ShouldAllowSettingProperties()
        {
            // Arrange
            var contract = new JsonSchemaContract
            {
                Schema = "{ \"type\": \"object\" }",
                ContentType = "application/xml",
                SchemaVersion = "v2.0"
            };

            // Assert
            Assert.Equal("{ \"type\": \"object\" }", contract.Schema);
            Assert.Equal("application/xml", contract.ContentType);
            Assert.Equal("v2.0", contract.SchemaVersion);
        }

        [Fact]
        public void JsonSchemaContract_Properties_CanStoreCustomData()
        {
            // Arrange
            var contract = new JsonSchemaContract();

            // Act
            contract.Properties["required"] = new[] { "name", "age" };
            contract.Properties["additionalProperties"] = false;

            // Assert
            Assert.Equal(new[] { "name", "age" }, contract.Properties["required"]);
            Assert.Equal(false, contract.Properties["additionalProperties"]);
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}
