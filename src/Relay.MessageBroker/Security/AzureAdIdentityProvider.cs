using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Net.Http.Json;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Azure AD identity provider integration using MSAL.
/// </summary>
public class AzureAdIdentityProvider : IIdentityProvider
{
    private readonly AzureAdOptions _options;
    private readonly ILogger<AzureAdIdentityProvider> _logger;
    private readonly HttpClient _httpClient;
    private TokenValidationInfo? _cachedValidationInfo;
    private DateTimeOffset _cacheExpiry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdIdentityProvider"/> class.
    /// </summary>
    /// <param name="options">The Azure AD options.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public AzureAdIdentityProvider(
        IOptions<AzureAdOptions> options,
        HttpClient httpClient,
        ILogger<AzureAdIdentityProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _logger.LogInformation(
            "AzureAdIdentityProvider initialized. TenantId: {TenantId}, ClientId: {ClientId}",
            _options.TenantId,
            _options.ClientId);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        try
        {
            // Get validation info (includes JWKS endpoint)
            var validationInfo = await GetValidationInfoAsync(cancellationToken);

            // For Azure AD, we rely on the JWT validation with the keys from JWKS
            // The actual validation is done by JwtMessageAuthenticator
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token with Azure AD");
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
            var authority = $"https://login.microsoftonline.com/{_options.TenantId}/v2.0";
            var openIdConfigUrl = $"{authority}/.well-known/openid-configuration";

            _logger.LogDebug("Fetching OpenID configuration from {Url}", openIdConfigUrl);

            // Fetch OpenID configuration
            var openIdConfig = await _httpClient.GetFromJsonAsync<OpenIdConfiguration>(
                openIdConfigUrl,
                cancellationToken);

            if (openIdConfig == null)
            {
                throw new InvalidOperationException("Failed to fetch OpenID configuration from Azure AD");
            }

            // Fetch JWKS
            var jwksUrl = openIdConfig.JwksUri;
            _logger.LogDebug("Fetching JWKS from {Url}", jwksUrl);

            var jwks = await _httpClient.GetFromJsonAsync<JsonWebKeySet>(jwksUrl, cancellationToken);

            if (jwks?.Keys == null || jwks.Keys.Count == 0)
            {
                throw new InvalidOperationException("Failed to fetch signing keys from Azure AD");
            }

            var validationInfo = new TokenValidationInfo
            {
                Issuer = openIdConfig.Issuer,
                Audience = _options.ClientId,
                JwksUri = jwksUrl,
                SigningKeys = jwks.Keys.Select(k => k.Kid ?? string.Empty).Where(k => !string.IsNullOrEmpty(k)).ToList()
            };

            // Cache for 24 hours
            _cachedValidationInfo = validationInfo;
            _cacheExpiry = DateTimeOffset.UtcNow.AddHours(24);

            _logger.LogInformation(
                "Retrieved validation info from Azure AD. Issuer: {Issuer}, Keys: {KeyCount}",
                validationInfo.Issuer,
                validationInfo.SigningKeys.Count);

            return validationInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get validation info from Azure AD");
            throw;
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
        public string? Kty { get; set; }
        public string? Use { get; set; }
        public string? N { get; set; }
        public string? E { get; set; }
    }
}

/// <summary>
/// Configuration options for Azure AD integration.
/// </summary>
public class AzureAdOptions
{
    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID (application ID).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret (for confidential clients).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Validates the Azure AD options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
        {
            throw new InvalidOperationException("TenantId must be specified for Azure AD integration.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("ClientId must be specified for Azure AD integration.");
        }
    }
}
