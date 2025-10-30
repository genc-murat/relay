using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Security;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SecurityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageEncryption_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageEncryption(options => options.EncryptionKey = "dGVzdC1rZXktdGhhdC1pcy0zMi1jaGFycw=="); // base64 encoded 32-char key

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var keyProvider = serviceProvider.GetService<IKeyProvider>();
        Assert.NotNull(keyProvider);
        Assert.IsType<EnvironmentVariableKeyProvider>(keyProvider);

        var encryptor = serviceProvider.GetService<IMessageEncryptor>();
        Assert.NotNull(encryptor);
        Assert.IsType<AesMessageEncryptor>(encryptor);

        var keyRotationManager = serviceProvider.GetService<KeyRotationManager>();
        Assert.NotNull(keyRotationManager);
    }

    [Fact]
    public void AddMessageEncryption_WithConfigureOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageEncryption(options =>
        {
            options.EnableEncryption = true;
            options.EncryptionKey = "test-key";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<SecurityOptions>>();
        Assert.NotNull(options);
        Assert.True(options.Value.EnableEncryption);
        Assert.Equal("test-key", options.Value.EncryptionKey);
    }

    [Fact]
    public void AddMessageEncryption_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageEncryption());
    }

    [Fact]
    public void AddMessageEncryption_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var chainedServices = services.AddMessageEncryption();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void AddMessageEncryptionWithKeyVault_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageEncryptionWithKeyVault(options => options.KeyVaultUrl = "https://test.vault.azure.net/");

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var keyProvider = serviceProvider.GetService<IKeyProvider>();
        Assert.NotNull(keyProvider);
        Assert.IsType<AzureKeyVaultKeyProvider>(keyProvider);

        var encryptor = serviceProvider.GetService<IMessageEncryptor>();
        Assert.NotNull(encryptor);
        Assert.IsType<AesMessageEncryptor>(encryptor);

        var keyRotationManager = serviceProvider.GetService<KeyRotationManager>();
        Assert.NotNull(keyRotationManager);
    }

    [Fact]
    public void AddMessageEncryptionWithKeyVault_WithConfigureOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageEncryptionWithKeyVault(options =>
        {
            options.EnableEncryption = true;
            options.KeyVaultUrl = "https://test.vault.azure.net/";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<SecurityOptions>>();
        Assert.NotNull(options);
        Assert.True(options.Value.EnableEncryption);
        Assert.Equal("https://test.vault.azure.net/", options.Value.KeyVaultUrl);
    }

    [Fact]
    public void AddMessageEncryptionWithKeyVault_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageEncryptionWithKeyVault());
    }

    [Fact]
    public void AddMessageEncryptionWithKeyVault_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var chainedServices = services.AddMessageEncryptionWithKeyVault();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void DecorateWithEncryption_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.DecorateWithEncryption());
    }

    [Fact]
    public void DecorateWithEncryption_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker>(new MockMessageBroker());

        // Act
        var chainedServices = services.DecorateWithEncryption();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void AddMessageAuthentication_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthentication();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var securityEventLogger = serviceProvider.GetService<SecurityEventLogger>();
        Assert.NotNull(securityEventLogger);

        var authenticator = serviceProvider.GetService<IMessageAuthenticator>();
        Assert.NotNull(authenticator);
        Assert.IsType<JwtMessageAuthenticator>(authenticator);
    }

    [Fact]
    public void AddMessageAuthentication_WithConfigureOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthentication(
            authOptions =>
            {
                authOptions.EnableAuthentication = true;
                authOptions.JwtIssuer = "test-issuer";
            },
            authzOptions =>
            {
                authzOptions.AllowByDefault = true;
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>();
        Assert.NotNull(authOptions);
        Assert.True(authOptions.Value.EnableAuthentication);
        Assert.Equal("test-issuer", authOptions.Value.JwtIssuer);

        var authzOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
        Assert.NotNull(authzOptions);
        Assert.True(authzOptions.Value.AllowByDefault);
    }

    [Fact]
    public void AddMessageAuthentication_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageAuthentication());
    }

    [Fact]
    public void AddMessageAuthentication_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var chainedServices = services.AddMessageAuthentication();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void AddMessageAuthenticationWithAzureAd_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthenticationWithAzureAd(
            authOptions => { },
            authzOptions => { },
            azureAdOptions =>
            {
                azureAdOptions.TenantId = "test-tenant";
                azureAdOptions.ClientId = "test-client";
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var securityEventLogger = serviceProvider.GetService<SecurityEventLogger>();
        Assert.NotNull(securityEventLogger);

        var authenticator = serviceProvider.GetService<IMessageAuthenticator>();
        Assert.NotNull(authenticator);
        Assert.IsType<JwtMessageAuthenticator>(authenticator);

        var identityProvider = serviceProvider.GetService<IIdentityProvider>();
        Assert.NotNull(identityProvider);
        Assert.IsType<AzureAdIdentityProvider>(identityProvider);
    }

    [Fact]
    public void AddMessageAuthenticationWithAzureAd_WithConfigureOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthenticationWithAzureAd(
            authOptions =>
            {
                authOptions.EnableAuthentication = true;
                authOptions.JwtIssuer = "test-issuer";
            },
            authzOptions =>
            {
                authzOptions.AllowByDefault = true;
            },
            azureAdOptions =>
            {
                azureAdOptions.TenantId = "test-tenant";
                azureAdOptions.ClientId = "test-client-id";
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>();
        Assert.NotNull(authOptions);
        Assert.True(authOptions.Value.EnableAuthentication);
        Assert.Equal("test-issuer", authOptions.Value.JwtIssuer);

        var authzOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
        Assert.NotNull(authzOptions);
        Assert.True(authzOptions.Value.AllowByDefault);

        var azureAdOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AzureAdOptions>>();
        Assert.NotNull(azureAdOptions);
        Assert.Equal("test-tenant", azureAdOptions.Value.TenantId);
        Assert.Equal("test-client-id", azureAdOptions.Value.ClientId);
    }

    [Fact]
    public void AddMessageAuthenticationWithAzureAd_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageAuthenticationWithAzureAd());
    }

    [Fact]
    public void AddMessageAuthenticationWithAzureAd_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var chainedServices = services.AddMessageAuthenticationWithAzureAd();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void AddMessageAuthenticationWithOAuth2_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthenticationWithOAuth2(
            authOptions => { },
            authzOptions => { },
            oauth2Options =>
            {
                oauth2Options.Authority = "https://test.auth.com";
                oauth2Options.Audience = "test-audience";
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var securityEventLogger = serviceProvider.GetService<SecurityEventLogger>();
        Assert.NotNull(securityEventLogger);

        var authenticator = serviceProvider.GetService<IMessageAuthenticator>();
        Assert.NotNull(authenticator);
        Assert.IsType<JwtMessageAuthenticator>(authenticator);

        var identityProvider = serviceProvider.GetService<IIdentityProvider>();
        Assert.NotNull(identityProvider);
        Assert.IsType<OAuth2IdentityProvider>(identityProvider);
    }

    [Fact]
    public void AddMessageAuthenticationWithOAuth2_WithConfigureOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageAuthenticationWithOAuth2(
            authOptions =>
            {
                authOptions.EnableAuthentication = true;
                authOptions.JwtIssuer = "test-issuer";
            },
            authzOptions =>
            {
                authzOptions.AllowByDefault = true;
            },
            oauth2Options =>
            {
                oauth2Options.Authority = "https://test.auth.com";
                oauth2Options.Audience = "test-audience";
                oauth2Options.ClientId = "test-client-id";
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var authOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthenticationOptions>>();
        Assert.NotNull(authOptions);
        Assert.True(authOptions.Value.EnableAuthentication);
        Assert.Equal("test-issuer", authOptions.Value.JwtIssuer);

        var authzOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
        Assert.NotNull(authzOptions);
        Assert.True(authzOptions.Value.AllowByDefault);

        var oauth2Options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<OAuth2Options>>();
        Assert.NotNull(oauth2Options);
        Assert.Equal("https://test.auth.com", oauth2Options.Value.Authority);
        Assert.Equal("test-audience", oauth2Options.Value.Audience);
        Assert.Equal("test-client-id", oauth2Options.Value.ClientId);
    }

    [Fact]
    public void AddMessageAuthenticationWithOAuth2_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageAuthenticationWithOAuth2());
    }

    [Fact]
    public void AddMessageAuthenticationWithOAuth2_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var chainedServices = services.AddMessageAuthenticationWithOAuth2();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void DecorateWithSecurity_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.DecorateWithSecurity());
    }

    [Fact]
    public void DecorateWithSecurity_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker>(new MockMessageBroker());

        // Act
        var chainedServices = services.DecorateWithSecurity();

        // Assert
        Assert.Same(services, chainedServices);
    }

    private class MockMessageBroker : IMessageBroker
    {
        public ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}