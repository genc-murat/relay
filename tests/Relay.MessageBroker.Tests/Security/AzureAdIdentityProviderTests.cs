using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Relay.MessageBroker.Security;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class AzureAdIdentityProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<AzureAdOptions>> _optionsMock;
    private readonly AzureAdOptions _azureAdOptions;
    private readonly Mock<ILogger<AzureAdIdentityProvider>> _loggerMock;

    public AzureAdIdentityProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

        _optionsMock = new Mock<IOptions<AzureAdOptions>>();
        _azureAdOptions = new AzureAdOptions
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };
        _optionsMock.Setup(o => o.Value).Returns(_azureAdOptions);

        _loggerMock = new Mock<ILogger<AzureAdIdentityProvider>>();
    }

    private HttpClient CreateMockHttpClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
    {
        var handler = new DelegatingHandlerStub(responseFactory);
        return new HttpClient(handler);
    }

    private class DelegatingHandlerStub : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responseFactory;

        public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var response = _responseFactory(request, cancellationToken);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, _httpClient, _loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureAdIdentityProvider(null!, _httpClient, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureAdIdentityProvider(_optionsMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureAdIdentityProvider(_optionsMock.Object, _httpClient, null!));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, _httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await provider.ValidateTokenAsync(null!));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWhiteSpaceToken_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, _httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await provider.ValidateTokenAsync("   "));
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenGetValidationInfoFails_ShouldReturnFalse()
    {
        // Arrange
        var httpClient = CreateMockHttpClient((request, token) => throw new HttpRequestException("Network error"));
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, httpClient, _loggerMock.Object);

        // Act
        var result = await provider.ValidateTokenAsync("some-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetValidationInfoAsync_WhenOpenIdConfigFails_ShouldThrowException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient((request, token) =>
        {
            if (request.RequestUri!.ToString().Contains("openid-configuration"))
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await provider.GetValidationInfoAsync());
    }

    [Fact]
    public async Task GetValidationInfoAsync_WhenJwksHasNoKeys_ShouldThrowException()
    {
        // Arrange
        var openIdConfig = new
        {
            issuer = "https://login.microsoftonline.com/test-tenant-id/v2.0",
            jwks_uri = "https://login.microsoftonline.com/test-tenant-id/discovery/v2.0/keys"
        };

        var jwks = new
        {
            keys = new object[0]
        };

        var httpClient = CreateMockHttpClient((request, token) =>
        {
            if (request.RequestUri!.ToString().Contains("openid-configuration"))
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(openIdConfig), System.Text.Encoding.UTF8, "application/json")
                };
            }
            else if (request.RequestUri!.ToString().Contains("keys"))
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(jwks), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });
        var provider = new AzureAdIdentityProvider(_optionsMock.Object, httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.GetValidationInfoAsync());
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowDuringValidation()
    {
        // Arrange
        var invalidOptions = new AzureAdOptions
        {
            TenantId = "",
            ClientId = "test-client-id"
        };
        var invalidOptionsMock = new Mock<IOptions<AzureAdOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new AzureAdIdentityProvider(invalidOptionsMock.Object, _httpClient, _loggerMock.Object));
    }
}
