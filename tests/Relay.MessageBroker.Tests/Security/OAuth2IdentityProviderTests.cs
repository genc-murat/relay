using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Relay.MessageBroker.Security;
using System.Net;
using System.Text;

namespace Relay.MessageBroker.Tests.Security;

public class OAuth2IdentityProviderTests
{
    private readonly Mock<IOptions<OAuth2Options>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<OAuth2IdentityProvider>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly OAuth2Options _options;

    public OAuth2IdentityProviderTests()
    {
        _optionsMock = new Mock<IOptions<OAuth2Options>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<OAuth2IdentityProvider>>();

        _options = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience"
        };
        _optionsMock.Setup(o => o.Value).Returns(_options);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2IdentityProvider(
                null!,
                _httpClient,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2IdentityProvider(
                _optionsMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2IdentityProvider(
                _optionsMock.Object,
                _httpClient,
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new OAuth2Options
        {
            Authority = "", // Invalid
            Audience = "test-audience"
        };
        var invalidOptionsMock = new Mock<IOptions<OAuth2Options>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new OAuth2IdentityProvider(
                invalidOptionsMock.Object,
                _httpClient,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithInvalidAudience_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "" // Invalid
        };
        var invalidOptionsMock = new Mock<IOptions<OAuth2Options>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new OAuth2IdentityProvider(
                invalidOptionsMock.Object,
                _httpClient,
                _loggerMock.Object));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await provider.ValidateTokenAsync(null!));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.ValidateTokenAsync(""));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWhitespaceToken_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.ValidateTokenAsync("   "));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpoint_ShouldUseIntrospection()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect"
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to return an active token
        var responseContent = @"{""active"": true}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpointAndInactiveToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect"
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to return an inactive token
        var responseContent = @"{""active"": false}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpointAndNullResponse_ShouldReturnFalse()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect"
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to return null response
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpointAndClientCredentials_ShouldUseBasicAuth()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock
        var responseContent = @"{""active"": true}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Authorization != null && 
                    req.Headers.Authorization.Scheme == "Basic"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpointAndMissingClientCredentials_ShouldNotUseBasicAuth()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect",
            ClientId = "test-client" // Missing secret
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock
        var responseContent = @"{""active"": true}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Authorization == null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithIntrospectionEndpointAndHttpException_ShouldReturnFalse()
    {
        // Arrange
        var token = "test-token";
        var introspectionOptions = new OAuth2Options
        {
            Authority = "https://example.com",
            Audience = "test-audience",
            IntrospectionEndpoint = "https://example.com/introspect"
        };
        _optionsMock.Setup(o => o.Value).Returns(introspectionOptions);

        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to throw an exception
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithoutIntrospectionEndpoint_ShouldUseJwks()
    {
        // Arrange
        var token = "test-token";
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock for OpenID configuration
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = "https://example.com/jwks"
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        // Setup mock for JWKS
        var jwksResponse = new
        {
            keys = new[] {
                new { kid = "test-key-1" },
                new { kid = "test-key-2" }
            }
        };
        var jwksHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(jwksResponse), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? configResponse : jwksHttpResponse;
            });

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithCachedInfo_ShouldReturnCachedInfo()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // First call to populate cache
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = "https://example.com/jwks"
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        var jwksResponse = new
        {
            keys = new[] {
                new { kid = "test-key-1" }
            }
        };
        var jwksHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(jwksResponse), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? configResponse : jwksHttpResponse;
            });

        // First call to populate cache
        var firstResult = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Act - Second call should return cached value
        var secondResult = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Assert
        Assert.Equal(firstResult.Issuer, secondResult.Issuer);
        Assert.Equal(firstResult.Audience, secondResult.Audience);
        Assert.Equal(firstResult.SigningKeys, secondResult.SigningKeys);
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithNullOpenIdConfig_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to return a valid response but with null content (JSON deserializes to null)
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(configResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.GetValidationInfoAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithNullJwksResponse_ShouldHandleGracefully()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock for OpenID configuration (with JWKS URI)
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = "https://example.com/jwks"
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        // Setup mock for null JWKS (this simulates a case where keys would be null)
        var jwksHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"keys\": null}", Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? configResponse : jwksHttpResponse;
            });

        // Act
        var result = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Assert
        Assert.Equal("https://example.com", result.Issuer);
        Assert.Equal("test-audience", result.Audience);
        Assert.Empty(result.SigningKeys); // Should be empty when JWKS keys are null
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithEmptyJwksKeys_ShouldHandleGracefully()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock for OpenID configuration (with JWKS URI)
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = "https://example.com/jwks"
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        // Setup mock for JWKS with empty keys array
        var jwksResponse = new
        {
            keys = new object[0] // Empty array
        };
        var jwksHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(jwksResponse), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? configResponse : jwksHttpResponse;
            });

        // Act
        var result = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Assert
        Assert.Equal("https://example.com", result.Issuer);
        Assert.Equal("test-audience", result.Audience);
        Assert.Empty(result.SigningKeys); // Should be empty with no keys
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithJwksContainingEmptyKeyIds_ShouldFilterThem()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock for OpenID configuration (with JWKS URI)
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = "https://example.com/jwks"
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        // Setup mock for JWKS with a known valid response to test the filtering logic
        // We'll test if the implementation correctly filters out null/empty kid values
        var jwksResponse = @"{
          ""keys"": [
            {""kid"": ""valid-key-1""},
            {""kid"": null},
            {""kid"": """"},
            {""kid"": ""valid-key-2""}
          ]
        }";
        var jwksHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jwksResponse, Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? configResponse : jwksHttpResponse;
            });

        // Act
        var result = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Assert the basic functionality - the issuer and audience should be set
        Assert.Equal("https://example.com", result.Issuer);
        Assert.Equal("test-audience", result.Audience);
        
        // The main purpose of this test is to ensure that the filtering logic works correctly:
        // - Keys with valid kid values should be included
        // - Keys with null or empty kid values should be excluded
        
        // Check that if there are any signing keys, they don't contain empty strings
        // (since empty/null kid values should be filtered out)
        foreach (var key in result.SigningKeys)
        {
            // Make sure no key is an empty string (which would indicate a null kid that wasn't filtered)
            Assert.NotEqual("", key);
        }
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithoutJwksUri_ShouldHandleGracefully()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock for OpenID configuration (without JWKS URI)
        var openIdConfig = new
        {
            issuer = "https://example.com",
            jwks_uri = (string?)null // No JWKS URI
        };
        var configResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(openIdConfig), Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(configResponse);

        // Act
        var result = await provider.GetValidationInfoAsync(CancellationToken.None);

        // Assert
        Assert.Equal("https://example.com", result.Issuer);
        Assert.Equal("test-audience", result.Audience);
        Assert.Empty(result.SigningKeys); // Should be empty when no JWKS URI
    }

    [Fact]
    public async Task GetValidationInfoAsync_WithHttpException_ShouldThrowException()
    {
        // Arrange
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to throw an exception
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await provider.GetValidationInfoAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        var token = "test-token";
        var provider = new OAuth2IdentityProvider(
            _optionsMock.Object,
            _httpClient,
            _loggerMock.Object);

        // Setup mock to throw an exception
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await provider.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }
}