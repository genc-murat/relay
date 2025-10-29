using System;
using System.Collections.Generic;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.SchemaDiscovery;

public class SchemaContextTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var requestType = typeof(string);
        var schemaVersion = "http://json-schema.org/draft-07/schema#";
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var context = new SchemaContext
        {
            RequestType = requestType,
            SchemaVersion = schemaVersion,
            Metadata = metadata,
            IsRequest = true
        };

        // Assert
        Assert.Equal(requestType, context.RequestType);
        Assert.Equal(schemaVersion, context.SchemaVersion);
        Assert.Equal(metadata, context.Metadata);
        Assert.True(context.IsRequest);
    }

    [Fact]
    public void SchemaVersion_CanBeNull()
    {
        // Act
        var context = new SchemaContext
        {
            RequestType = typeof(string),
            SchemaVersion = null,
            IsRequest = true
        };

        // Assert
        Assert.Null(context.SchemaVersion);
    }

    [Fact]
    public void Metadata_CanBeNull()
    {
        // Act
        var context = new SchemaContext
        {
            RequestType = typeof(string),
            Metadata = null,
            IsRequest = true
        };

        // Assert
        Assert.Null(context.Metadata);
    }

    [Fact]
    public void IsRequest_CanBeFalse()
    {
        // Act
        var context = new SchemaContext
        {
            RequestType = typeof(string),
            IsRequest = false
        };

        // Assert
        Assert.False(context.IsRequest);
    }
}
