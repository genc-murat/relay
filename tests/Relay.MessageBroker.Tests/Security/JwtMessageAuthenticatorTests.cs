using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Relay.MessageBroker.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class JwtMessageAuthenticatorTests
{
    private readonly Mock<IOptions<AuthenticationOptions>> _authOptionsMock;
    private readonly Mock<IOptions<AuthorizationOptions>> _authzOptionsMock;
    private readonly Mock<ILogger<JwtMessageAuthenticator>> _loggerMock;
    private readonly Mock<ILogger<SecurityEventLogger>> _securityLoggerMock;
    private readonly AuthenticationOptions _authOptions;
    private readonly AuthorizationOptions _authzOptions;
    private readonly SecurityEventLogger _securityEventLogger;

    public JwtMessageAuthenticatorTests()
    {
        _authOptionsMock = new Mock<IOptions<AuthenticationOptions>>();
        _authzOptionsMock = new Mock<IOptions<AuthorizationOptions>>();
        _loggerMock = new Mock<ILogger<JwtMessageAuthenticator>>();
        _securityLoggerMock = new Mock<ILogger<SecurityEventLogger>>();

        _authOptions = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience",
            JwtSigningKey = Convert.ToBase64String(new byte[32]), // Dummy key
            TokenCacheTtl = TimeSpan.FromMinutes(5)
        };
        _authOptionsMock.Setup(o => o.Value).Returns(_authOptions);

        _authzOptions = new AuthorizationOptions
        {
            AllowByDefault = false,
            RoleClaimType = "role",
            PublishPermissions = new Dictionary<string, List<string>>
            {
                ["admin"] = new List<string> { "*" },
                ["publisher"] = new List<string> { "news", "events" }
            },
            SubscribePermissions = new Dictionary<string, List<string>>
            {
                ["admin"] = new List<string> { "*" },
                ["subscriber"] = new List<string> { "news" }
            }
        };
        _authzOptionsMock.Setup(o => o.Value).Returns(_authzOptions);

        _securityEventLogger = new SecurityEventLogger(_securityLoggerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Assert
        Assert.NotNull(authenticator);
    }

    [Fact]
    public void Constructor_WithNullAuthOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JwtMessageAuthenticator(
                null!,
                _authzOptionsMock.Object,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullAuthzOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JwtMessageAuthenticator(
                _authOptionsMock.Object,
                null!,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JwtMessageAuthenticator(
                _authOptionsMock.Object,
                _authzOptionsMock.Object,
                null!,
                _securityEventLogger));
    }

    [Fact]
    public void Constructor_WithNullSecurityEventLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new JwtMessageAuthenticator(
                _authOptionsMock.Object,
                _authzOptionsMock.Object,
                _loggerMock.Object,
                null!));
    }

    [Fact]
    public void Constructor_WithInvalidAuthOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidOptions = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtIssuer = "", // Invalid
            JwtAudience = "test-audience",
            JwtSigningKey = Convert.ToBase64String(new byte[32])
        };
        var invalidOptionsMock = new Mock<IOptions<AuthenticationOptions>>();
        invalidOptionsMock.Setup(o => o.Value).Returns(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new JwtMessageAuthenticator(
                invalidOptionsMock.Object,
                _authzOptionsMock.Object,
                _loggerMock.Object,
                _securityEventLogger));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authenticator.ValidateTokenAsync(null!));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await authenticator.ValidateTokenAsync(""));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWhitespaceToken_ShouldThrowArgumentException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await authenticator.ValidateTokenAsync("   "));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtToken();

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateExpiredJwtToken();

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongIssuer_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateJwtTokenWithWrongIssuer();

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongAudience_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateJwtTokenWithWrongAudience();

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateJwtTokenWithInvalidSignature();

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }



    [Fact]
    public async Task ValidateTokenAsync_WithCachedValidResult_ShouldReturnCachedResult()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtToken();

        // First call to cache the result
        await authenticator.ValidateTokenAsync(token);

        // Act - Second call should use cache
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithCachedInvalidResult_ShouldReturnCachedResult()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var invalidToken = "invalid.jwt.token";

        // First call to cache the result
        await authenticator.ValidateTokenAsync(invalidToken);

        // Act - Second call should use cache
        var result = await authenticator.ValidateTokenAsync(invalidToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authenticator.AuthorizeAsync(null!, "publish"));
    }

    [Fact]
    public async Task AuthorizeAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtToken();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await authenticator.AuthorizeAsync(token, null!));
    }

    [Fact]
    public async Task AuthorizeAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var invalidToken = "invalid.jwt.token";

        // Act
        var result = await authenticator.AuthorizeAsync(invalidToken, "publish");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndAdminRoleForPublish_ShouldReturnTrue()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("admin");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndPublisherRoleForPublish_ShouldReturnTrue()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("publisher");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndSubscriberRoleForPublish_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("subscriber");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndAdminRoleForSubscribe_ShouldReturnTrue()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("admin");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "subscribe");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndSubscriberRoleForSubscribe_ShouldReturnTrue()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("subscriber");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "subscribe");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndPublisherRoleForSubscribe_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("publisher");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "subscribe");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndUnknownRole_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("unknown");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndNoRoles_ShouldReturnAllowByDefault()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithNoRoles();

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.Equal(_authzOptions.AllowByDefault, result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidTokenAndCustomRoleClaimType_ShouldUseCustomClaim()
    {
        // Arrange
        var customAuthzOptions = new AuthorizationOptions
        {
            AllowByDefault = false,
            RoleClaimType = "custom-role",
            PublishPermissions = new Dictionary<string, List<string>>
            {
                ["custom-admin"] = new List<string> { "*" }
            }
        };
        var customAuthzOptionsMock = new Mock<IOptions<AuthorizationOptions>>();
        customAuthzOptionsMock.Setup(o => o.Value).Returns(customAuthzOptions);

        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            customAuthzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithCustomRoleClaim("custom-admin");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithUnknownOperation_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("admin");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "unknown");

        // Assert
        Assert.False(result);
    }



    [Fact]
    public async Task ValidateTokenAsync_WithRsaPublicKey_ShouldValidateSuccessfully()
    {
        // Arrange
        var rsa = RSA.Create();
        var rsaKeyPem = rsa.ExportRSAPublicKeyPem();

        var rsaAuthOptions = new AuthenticationOptions
        {
            EnableAuthentication = true,
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience",
            JwtPublicKey = rsaKeyPem,
            TokenCacheTtl = TimeSpan.FromMinutes(5)
        };
        var rsaAuthOptionsMock = new Mock<IOptions<AuthenticationOptions>>();
        rsaAuthOptionsMock.Setup(o => o.Value).Returns(rsaAuthOptions);

        var authenticator = new JwtMessageAuthenticator(
            rsaAuthOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRsa(rsa);

        // Act
        var result = await authenticator.ValidateTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithMalformedToken_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var malformedToken = "malformed.jwt.token";

        // Act
        var result = await authenticator.ValidateTokenAsync(malformedToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNonJwtToken_ShouldReturnFalse()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        // Create a token that looks like JWT but isn't
        var nonJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.not-a-jwt-payload.signature";

        // Act
        var result = await authenticator.ValidateTokenAsync(nonJwtToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeAsync_WithEmptyOperation_ShouldThrowArgumentException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtToken();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await authenticator.AuthorizeAsync(token, ""));
    }

    [Fact]
    public async Task AuthorizeAsync_WithWhitespaceOperation_ShouldThrowArgumentException()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtToken();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await authenticator.AuthorizeAsync(token, "   "));
    }

    [Fact]
    public async Task AuthorizeAsync_WithNoPermissionsConfigured_ShouldReturnAllowByDefault()
    {
        // Arrange
        var noPermissionsAuthzOptions = new AuthorizationOptions
        {
            AllowByDefault = true,
            RoleClaimType = "role",
            PublishPermissions = new Dictionary<string, List<string>>(), // Empty permissions
            SubscribePermissions = new Dictionary<string, List<string>>() // Empty permissions
        };
        var noPermissionsAuthzOptionsMock = new Mock<IOptions<AuthorizationOptions>>();
        noPermissionsAuthzOptionsMock.Setup(o => o.Value).Returns(noPermissionsAuthzOptions);

        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            noPermissionsAuthzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithRole("any-role");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result); // Should return AllowByDefault (true)
    }

    [Fact]
    public async Task AuthorizeAsync_WithMultipleRoles_ShouldCheckAllRoles()
    {
        // Arrange
        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            _authzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithMultipleRoles(new[] { "subscriber", "admin" });

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result); // Should succeed because admin role has permissions
    }

    [Fact]
    public async Task AuthorizeAsync_WithStandardRoleClaim_ShouldUseStandardClaim()
    {
        // Arrange
        var standardRoleAuthzOptions = new AuthorizationOptions
        {
            AllowByDefault = false,
            RoleClaimType = "custom-role", // Different from standard
            PublishPermissions = new Dictionary<string, List<string>>
            {
                ["standard-admin"] = new List<string> { "*" }
            }
        };
        var standardRoleAuthzOptionsMock = new Mock<IOptions<AuthorizationOptions>>();
        standardRoleAuthzOptionsMock.Setup(o => o.Value).Returns(standardRoleAuthzOptions);

        var authenticator = new JwtMessageAuthenticator(
            _authOptionsMock.Object,
            standardRoleAuthzOptionsMock.Object,
            _loggerMock.Object,
            _securityEventLogger);

        var token = CreateValidJwtTokenWithStandardRoleClaim("standard-admin");

        // Act
        var result = await authenticator.AuthorizeAsync(token, "publish");

        // Assert
        Assert.True(result); // Should find role in standard claim type
    }

    // Helper methods for creating test tokens
    private string CreateValidJwtToken()
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateExpiredJwtToken()
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(-1), // Expired
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateJwtTokenWithWrongIssuer()
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "wrong-issuer",
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateJwtTokenWithWrongAudience()
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: "wrong-audience",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateJwtTokenWithInvalidSignature()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-secret-key-wrong-secret-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }



    private string CreateValidJwtTokenWithRole(string role)
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(_authzOptions.RoleClaimType, role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateValidJwtTokenWithNoRoles()
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateValidJwtTokenWithCustomRoleClaim(string role)
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("custom-role", role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }



    private string CreateValidJwtTokenWithRsa(RSA rsa)
    {
        var key = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateValidJwtTokenWithMultipleRoles(string[] roles)
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(_authzOptions.RoleClaimType, role));
        }

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateValidJwtTokenWithStandardRoleClaim(string role)
    {
        var keyBytes = Convert.FromBase64String(_authOptions.JwtSigningKey!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role) // Standard role claim
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}