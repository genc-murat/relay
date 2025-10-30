using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Relay.MessageBroker.Security;

/// <summary>
/// JWT-based message authenticator with token validation and caching.
/// </summary>
public class JwtMessageAuthenticator : IMessageAuthenticator
{
    private readonly AuthenticationOptions _authOptions;
    private readonly AuthorizationOptions _authzOptions;
    private readonly ILogger<JwtMessageAuthenticator> _logger;
    private readonly SecurityEventLogger _securityEventLogger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;
    private readonly ConcurrentDictionary<string, CachedTokenValidation> _tokenCache;
    private readonly Timer _cacheCleanupTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtMessageAuthenticator"/> class.
    /// </summary>
    /// <param name="authOptions">The authentication options.</param>
    /// <param name="authzOptions">The authorization options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="securityEventLogger">The security event logger.</param>
    public JwtMessageAuthenticator(
        IOptions<AuthenticationOptions> authOptions,
        IOptions<AuthorizationOptions> authzOptions,
        ILogger<JwtMessageAuthenticator> logger,
        SecurityEventLogger securityEventLogger)
    {
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _authzOptions = authzOptions?.Value ?? throw new ArgumentNullException(nameof(authzOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityEventLogger = securityEventLogger ?? throw new ArgumentNullException(nameof(securityEventLogger));

        _authOptions.Validate();
        _authzOptions.Validate();

        _tokenHandler = new JwtSecurityTokenHandler();
        _validationParameters = CreateValidationParameters();
        _tokenCache = new ConcurrentDictionary<string, CachedTokenValidation>();

        // Setup cache cleanup timer to run every minute
        _cacheCleanupTimer = new Timer(
            CleanupExpiredTokens,
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));

        _logger.LogInformation(
            "JwtMessageAuthenticator initialized. Issuer: {Issuer}, Audience: {Audience}",
            _authOptions.JwtIssuer,
            _authOptions.JwtAudience);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        // Check cache first
        if (_tokenCache.TryGetValue(token, out var cachedValidation))
        {
            if (cachedValidation.ExpiresAt > DateTimeOffset.UtcNow)
            {
                _logger.LogTrace("Token validation result retrieved from cache");
                return cachedValidation.IsValid;
            }

            // Remove expired entry
            _tokenCache.TryRemove(token, out _);
        }

        try
        {
            // Validate token
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Additional validation checks
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Token is not a valid JWT token");
                CacheValidationResult(token, false);
                return false;
            }

            // Verify signature algorithm
            if (!IsValidSignatureAlgorithm(jwtToken.Header.Alg))
            {
                _logger.LogWarning("Token uses invalid signature algorithm: {Algorithm}", jwtToken.Header.Alg);
                CacheValidationResult(token, false);
                return false;
            }

            // Verify expiration
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token has expired at {ExpirationTime}", jwtToken.ValidTo);
                CacheValidationResult(token, false);
                return false;
            }

            // Verify issuer
            if (!string.Equals(jwtToken.Issuer, _authOptions.JwtIssuer, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Token issuer mismatch. Expected: {Expected}, Actual: {Actual}",
                    _authOptions.JwtIssuer,
                    jwtToken.Issuer);
                CacheValidationResult(token, false);
                return false;
            }

            // Verify audience
            if (!jwtToken.Audiences.Any(a => string.Equals(a, _authOptions.JwtAudience, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Token audience mismatch. Expected: {Expected}, Actual: {Actual}",
                    _authOptions.JwtAudience,
                    string.Join(", ", jwtToken.Audiences));
                CacheValidationResult(token, false);
                return false;
            }

            _logger.LogDebug("Token validated successfully for subject: {Subject}", principal.Identity?.Name);
            _securityEventLogger.LogAuthenticationSuccess(principal.Identity?.Name ?? "Unknown");
            CacheValidationResult(token, true, principal);
            return true;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            _securityEventLogger.LogAuthenticationFailure("Token expired");
            CacheValidationResult(token, false);
            return false;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token has invalid signature");
            _securityEventLogger.LogAuthenticationFailure("Invalid signature");
            CacheValidationResult(token, false);
            return false;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            _securityEventLogger.LogTokenValidationError(ex.Message);
            CacheValidationResult(token, false);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            _securityEventLogger.LogAuthenticationFailure($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<bool> AuthorizeAsync(string token, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        // First validate the token
        if (!await ValidateTokenAsync(token, cancellationToken))
        {
            _logger.LogWarning("Authorization failed: token validation failed");
            return false;
        }

        try
        {
            // Get cached principal or parse token
            ClaimsPrincipal? principal = null;
            if (_tokenCache.TryGetValue(token, out var cachedValidation) && cachedValidation.Principal != null)
            {
                principal = cachedValidation.Principal;
            }
            else
            {
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                principal = new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
            }

            // Extract roles from claims
            var roles = ExtractRoles(principal);

            if (roles.Count == 0)
            {
                _logger.LogWarning("No roles found in token for authorization");
                return _authzOptions.AllowByDefault;
            }

            // Check permissions based on operation
            var hasPermission = operation.ToLowerInvariant() switch
            {
                "publish" => CheckPermissions(roles, _authzOptions.PublishPermissions),
                "subscribe" => CheckPermissions(roles, _authzOptions.SubscribePermissions),
                _ => false
            };

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "Authorization failed for operation '{Operation}'. Roles: {Roles}",
                    operation,
                    string.Join(", ", roles));
                _securityEventLogger.LogUnauthorizedAccess(operation, "Insufficient permissions", roles);
            }
            else
            {
                _logger.LogDebug(
                    "Authorization successful for operation '{Operation}'. Roles: {Roles}",
                    operation,
                    string.Join(", ", roles));
                _securityEventLogger.LogAuthorizedAccess(operation, roles);
            }

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during authorization for operation '{Operation}'", operation);
            return false;
        }
    }

    /// <summary>
    /// Creates token validation parameters based on configuration.
    /// </summary>
    private TokenValidationParameters CreateValidationParameters()
    {
        SecurityKey? signingKey = null;

        // Configure signing key
        if (!string.IsNullOrWhiteSpace(_authOptions.JwtSigningKey))
        {
            // Symmetric key
            var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey);
            signingKey = new SymmetricSecurityKey(keyBytes);
        }
        else if (!string.IsNullOrWhiteSpace(_authOptions.JwtPublicKey))
        {
            // Asymmetric key (RSA)
            signingKey = LoadRsaPublicKey(_authOptions.JwtPublicKey);
        }

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _authOptions.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _authOptions.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
        };
    }

    /// <summary>
    /// Loads an RSA public key from PEM format.
    /// </summary>
    private SecurityKey LoadRsaPublicKey(string publicKeyPem)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return new RsaSecurityKey(rsa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load RSA public key");
            throw new InvalidOperationException("Failed to load RSA public key", ex);
        }
    }

    /// <summary>
    /// Validates the signature algorithm.
    /// </summary>
    private bool IsValidSignatureAlgorithm(string algorithm)
    {
        // Only allow secure algorithms
        var allowedAlgorithms = new[]
        {
            SecurityAlgorithms.HmacSha256,
            SecurityAlgorithms.HmacSha384,
            SecurityAlgorithms.HmacSha512,
            SecurityAlgorithms.RsaSha256,
            SecurityAlgorithms.RsaSha384,
            SecurityAlgorithms.RsaSha512,
            SecurityAlgorithms.EcdsaSha256,
            SecurityAlgorithms.EcdsaSha384,
            SecurityAlgorithms.EcdsaSha512
        };

        return allowedAlgorithms.Contains(algorithm);
    }

    /// <summary>
    /// Extracts roles from the claims principal.
    /// </summary>
    private List<string> ExtractRoles(ClaimsPrincipal principal)
    {
        var roles = new List<string>();

        // Get role claims
        var roleClaims = principal.FindAll(_authzOptions.RoleClaimType);
        foreach (var claim in roleClaims)
        {
            if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                roles.Add(claim.Value);
            }
        }

        // Also check standard role claim type
        if (_authzOptions.RoleClaimType != ClaimTypes.Role)
        {
            var standardRoleClaims = principal.FindAll(ClaimTypes.Role);
            foreach (var claim in standardRoleClaims)
            {
                if (!string.IsNullOrWhiteSpace(claim.Value) && !roles.Contains(claim.Value))
                {
                    roles.Add(claim.Value);
                }
            }
        }

        return roles;
    }

    /// <summary>
    /// Checks if any of the user's roles have the required permissions.
    /// </summary>
    private bool CheckPermissions(List<string> userRoles, Dictionary<string, List<string>> permissions)
    {
        // If no permissions are configured, use default behavior
        if (permissions.Count == 0)
        {
            return _authzOptions.AllowByDefault;
        }

        // Check if any user role has permissions
        foreach (var role in userRoles)
        {
            if (permissions.ContainsKey(role))
            {
                // Role has permissions defined
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Caches the token validation result.
    /// </summary>
    private void CacheValidationResult(string token, bool isValid, ClaimsPrincipal? principal = null)
    {
        var cacheEntry = new CachedTokenValidation
        {
            IsValid = isValid,
            Principal = principal,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_authOptions.TokenCacheTtl)
        };

        _tokenCache.AddOrUpdate(token, cacheEntry, (_, _) => cacheEntry);
    }

    /// <summary>
    /// Cleans up expired tokens from the cache.
    /// </summary>
    private void CleanupExpiredTokens(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _tokenCache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _tokenCache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogTrace("Cleaned up {Count} expired tokens from cache", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Cached token validation result.
    /// </summary>
    private class CachedTokenValidation
    {
        public bool IsValid { get; set; }
        public ClaimsPrincipal? Principal { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
