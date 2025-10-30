using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class TenantIdExtractorTests
{
    [Fact]
    public void ExtractFromHeaders_WithTenantIdHeader_ShouldReturnTenantId()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = "tenant-123"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Equal("tenant-123", result);
    }

    [Fact]
    public void ExtractFromHeaders_WithXTenantIdHeader_ShouldReturnTenantId()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["X-Tenant-Id"] = "tenant-456"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Equal("tenant-456", result);
    }

    [Fact]
    public void ExtractFromHeaders_WithXTenantHeader_ShouldReturnTenantId()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["X-Tenant"] = "tenant-789"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Equal("tenant-789", result);
    }

    [Fact]
    public void ExtractFromHeaders_WithTenantIdUnderscoreHeader_ShouldReturnTenantId()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["tenant_id"] = "tenant-999"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Equal("tenant-999", result);
    }

    [Fact]
    public void ExtractFromHeaders_WithNullHeaders_ShouldReturnNull()
    {
        // Arrange & Act
        var result = TenantIdExtractor.ExtractFromHeaders(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromHeaders_WithEmptyHeaders_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>();

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromHeaders_WithNoTenantHeaders_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = "Bearer token",
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromHeaders_WithNullTenantId_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = null
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromHeaders_WithEmptyTenantId_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = ""
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromHeaders_WithWhitespaceTenantId_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = "   "
        };

        // Act
        var result = TenantIdExtractor.ExtractFromHeaders(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_WithTenantIdHeader_ShouldReturnTenantId()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = "tenant-123"
        };

        // Act
        var result = TenantIdExtractor.Extract(headers);

        // Assert
        Assert.Equal("tenant-123", result);
    }

    [Fact]
    public void Extract_WithNoHeaders_ShouldReturnDefaultTenantId()
    {
        // Arrange & Act
        var result = TenantIdExtractor.Extract(null, "default-tenant");

        // Assert
        Assert.Equal("default-tenant", result);
    }

    [Fact]
    public void Extract_WithDefaultTenantId_ShouldReturnDefault()
    {
        // Arrange
        var headers = new Dictionary<string, object?>();

        // Act
        var result = TenantIdExtractor.Extract(headers, "my-default");

        // Assert
        Assert.Equal("my-default", result);
    }

    [Fact]
    public void Extract_WithEmptyDefaultTenantId_ShouldReturnEmptyString()
    {
        // Arrange
        var headers = new Dictionary<string, object?>();

        // Act
        var result = TenantIdExtractor.Extract(headers, "");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithValidJwt_ShouldExtractTenantId()
    {
        // Arrange - Create a simple JWT payload with tenant_id
        var payload = @"{""sub"":""user123"",""tenant_id"":""tenant-456"",""exp"":1234567890}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = $"Bearer {jwt}"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Equal("tenant-456", result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithTidClaim_ShouldExtractTenantId()
    {
        // Arrange - Create a simple JWT payload with tid
        var payload = @"{""sub"":""user123"",""tid"":""tenant-789"",""exp"":1234567890}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = $"Bearer {jwt}"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Equal("tenant-789", result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithBearerPrefix_ShouldExtractTenantId()
    {
        // Arrange
        var payload = @"{""tenant_id"":""tenant-123""}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = $"Bearer {jwt}"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Equal("tenant-123", result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithoutBearerPrefix_ShouldExtractTenantId()
    {
        // Arrange
        var payload = @"{""tenant_id"":""tenant-123""}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = jwt
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Equal("tenant-123", result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithInvalidJwt_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = "Bearer invalid.jwt.token"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithNoAuthorizationHeader_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFromJwtClaims_WithNullAuthorizationHeader_ShouldReturnNull()
    {
        // Arrange
        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = null
        };

        // Act
        var result = TenantIdExtractor.ExtractFromJwtClaims(headers);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_WithJwtTenantId_ShouldPreferHeadersOverJwt()
    {
        // Arrange - Headers have tenant_id, JWT also has tenant_id
        var payload = @"{""tenant_id"":""jwt-tenant""}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["TenantId"] = "header-tenant",
            ["Authorization"] = $"Bearer {jwt}"
        };

        // Act
        var result = TenantIdExtractor.Extract(headers);

        // Assert - Should prefer header over JWT
        Assert.Equal("header-tenant", result);
    }

    [Fact]
    public void Extract_WithOnlyJwtTenantId_ShouldReturnJwtTenantId()
    {
        // Arrange
        var payload = @"{""tenant_id"":""jwt-tenant""}";
        var base64Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        var jwt = $"header.{base64Payload}.signature";

        var headers = new Dictionary<string, object?>
        {
            ["Authorization"] = $"Bearer {jwt}"
        };

        // Act
        var result = TenantIdExtractor.Extract(headers);

        // Assert
        Assert.Equal("jwt-tenant", result);
    }
}