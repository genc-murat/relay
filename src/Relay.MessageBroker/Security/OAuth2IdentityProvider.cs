using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Generic OAuth2 identity provider integration.
/// </summary>
public class OAuth2IdentityProvider : IIdentityProvider
{
    private readonly OAuth2Options _options;
    private readonly ILogger<OAuth2IdentityProvider> _logger;
    private readonly HttpClient _httpClient;
    private TokenValidationInfo? _cachedValidationInfo;
    private DateTimeOffset _cacheExpiry;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2IdentityProvider"/> class.
    /// </summary>
    /// <param name="options">The OAuth2 options.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public OAuth2IdentityProvider(
        IOptions<OAuth2Options> options,
        HttpClient httpClient,
        ILogger<OAuth2IdentityProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "OAuth2IdentityProvider initialized. Authority: {Authority}",
            _options.Authority);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            // If introspection endpoint is configured, use it
            if (!string.IsNullOrWhiteSpace(_options.IntrospectionEndpoint))
            {
                return await IntrospectTokenAsync(token, cancellationToken);
            }

            // Otherwise, rely on JWT validation with JWKS
            var validationInfo = await GetValidationInfoAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token with OAuth2 provider");
            return false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<TokenValidationInfo> GetValidationInfoAsync(CancellationToken cancellationToken = default)
    {
        // Return cached info if still valid
        if (_cachedValidationInfo != null && DateTimeOffset.UtcNow < _cacheExpiry)
        {
            return _cachedValidationInfo;
        }

        try
        {
            // Construct the OpenID configuration URL
            var openIdConfigUrl = $"{_options.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            _logger.LogDebug("Fetching OpenID configuration from {Url}", openIdConfigUrl);

            // Fetch OpenID configuration
            var openIdConfig = await _httpClient.GetFromJsonAsync<OpenIdConfiguration>(
                openIdConfigUrl,
                cancellationToken);

            if (openIdConfig == null)
            {
                throw new InvalidOperationException("Failed to fetch OpenID configuration from OAuth2 provider");
            }

            // Fetch JWKS if available
            var signingKeys = new List<string>();
            if (!string.IsNullOrWhiteSpace(openIdConfig.JwksUri))
            {
                _logger.LogDebug("Fetching JWKS from {Url}", openIdConfig.JwksUri);

                var jwks = await _httpClient.GetFromJsonAsync<JsonWebKeySet>(
                    openIdConfig.JwksUri,
                    cancellationToken);

                if (jwks?.Keys != null)
                {
                    signingKeys = jwks.Keys
                        .Select(k => k.Kid ?? string.Empty)
                        .Where(k => !string.IsNullOrEmpty(k))
                        .ToList();
                }
            }

            var validationInfo = new TokenValidationInfo
            {
                Issuer = openIdConfig.Issuer,
                Audience = _options.Audience,
                JwksUri = openIdConfig.JwksUri,
                SigningKeys = signingKeys
            };

            // Cache for 24 hours
            _cachedValidationInfo = validationInfo;
            _cacheExpiry = DateTimeOffset.UtcNow.AddHours(24);

            _logger.LogInformation(
                "Retrieved validation info from OAuth2 provider. Issuer: {Issuer}, Keys: {KeyCount}",
                validationInfo.Issuer,
                validationInfo.SigningKeys.Count);

            return validationInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get validation info from OAuth2 provider");
            throw;
        }
    }

    /// <summary>
    /// Introspects a token using the OAuth2 introspection endpoint.
    /// </summary>
    private async ValueTask<bool> IntrospectTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _options.IntrospectionEndpoint);

            // Add client credentials if configured
            if (!string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            // Add token to request body
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", token)
            });
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var introspectionResponse = await response.Content.ReadFromJsonAsync<IntrospectionResponse>(cancellationToken);

            return introspectionResponse?.Active ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to introspect token");
            return false;
        }
    }

    /// <summary>
    /// OpenID configuration response model.
    /// </summary>
    private class OpenIdConfiguration
    {
        public string Issuer { get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
    }

    /// <summary>
    /// JSON Web Key Set response model.
    /// </summary>
    private class JsonWebKeySet
    {
        public List<JsonWebKey> Keys { get; set; } = new();
    }

    /// <summary>
    /// JSON Web Key model.
    /// </summary>
    private class JsonWebKey
    {
        public string? Kid { get; set; }
    }

    /// <summary>
    /// Token introspection response model.
    /// </summary>
    private class IntrospectionResponse
    {
        public bool Active { get; set; }
    }
}

/// <summary>
/// Configuration options for OAuth2 integration.
/// </summary>
public class OAuth2Options
{
    /// <summary>
    /// Gets or sets the OAuth2 authority URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the token introspection endpoint URL.
    /// </summary>
    public string? IntrospectionEndpoint { get; set; }

    /// <summary>
    /// Validates the OAuth2 options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            throw new InvalidOperationException("Authority must be specified for OAuth2 integration.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Audience must be specified for OAuth2 integration.");
        }
    }
}
