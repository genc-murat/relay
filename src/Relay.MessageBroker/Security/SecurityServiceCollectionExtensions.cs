using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Extension methods for registering message security services.
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Adds message encryption services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure security options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageEncryption(
        this IServiceCollection services,
        Action<SecurityOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register key provider (default to environment variable provider)
        services.TryAddSingleton<IKeyProvider, EnvironmentVariableKeyProvider>();

        // Register encryptor
        services.TryAddSingleton<IMessageEncryptor, AesMessageEncryptor>();

        // Register key rotation manager
        services.TryAddSingleton<KeyRotationManager>();

        return services;
    }

    /// <summary>
    /// Adds message encryption services with Azure Key Vault support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure security options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageEncryptionWithKeyVault(
        this IServiceCollection services,
        Action<SecurityOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register Azure Key Vault key provider
        services.TryAddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();

        // Register encryptor
        services.TryAddSingleton<IMessageEncryptor, AesMessageEncryptor>();

        // Register key rotation manager
        services.TryAddSingleton<KeyRotationManager>();

        return services;
    }

    /// <summary>
    /// Decorates the IMessageBroker with encryption capabilities.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateWithEncryption(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Decorate<IMessageBroker, EncryptionMessageBrokerDecorator>();

        return services;
    }

    /// <summary>
    /// Adds message authentication and authorization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAuthOptions">Optional action to configure authentication options.</param>
    /// <param name="configureAuthzOptions">Optional action to configure authorization options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageAuthentication(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configureAuthOptions = null,
        Action<AuthorizationOptions>? configureAuthzOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configureAuthOptions != null)
        {
            services.Configure(configureAuthOptions);
        }

        if (configureAuthzOptions != null)
        {
            services.Configure(configureAuthzOptions);
        }

        // Register security event logger
        services.TryAddSingleton<SecurityEventLogger>();

        // Register authenticator
        services.TryAddSingleton<IMessageAuthenticator, JwtMessageAuthenticator>();

        return services;
    }

    /// <summary>
    /// Adds message authentication with Azure AD integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAuthOptions">Optional action to configure authentication options.</param>
    /// <param name="configureAuthzOptions">Optional action to configure authorization options.</param>
    /// <param name="configureAzureAdOptions">Optional action to configure Azure AD options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageAuthenticationWithAzureAd(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configureAuthOptions = null,
        Action<AuthorizationOptions>? configureAuthzOptions = null,
        Action<AzureAdOptions>? configureAzureAdOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add base authentication services
        services.AddMessageAuthentication(configureAuthOptions, configureAuthzOptions);

        // Configure Azure AD options
        if (configureAzureAdOptions != null)
        {
            services.Configure(configureAzureAdOptions);
        }

        // Register Azure AD identity provider
        services.TryAddSingleton<IIdentityProvider, AzureAdIdentityProvider>();

        // Register HttpClient for Azure AD provider
        services.AddHttpClient<AzureAdIdentityProvider>();

        return services;
    }

    /// <summary>
    /// Adds message authentication with OAuth2 integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureAuthOptions">Optional action to configure authentication options.</param>
    /// <param name="configureAuthzOptions">Optional action to configure authorization options.</param>
    /// <param name="configureOAuth2Options">Optional action to configure OAuth2 options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageAuthenticationWithOAuth2(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configureAuthOptions = null,
        Action<AuthorizationOptions>? configureAuthzOptions = null,
        Action<OAuth2Options>? configureOAuth2Options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add base authentication services
        services.AddMessageAuthentication(configureAuthOptions, configureAuthzOptions);

        // Configure OAuth2 options
        if (configureOAuth2Options != null)
        {
            services.Configure(configureOAuth2Options);
        }

        // Register OAuth2 identity provider
        services.TryAddSingleton<IIdentityProvider, OAuth2IdentityProvider>();

        // Register HttpClient for OAuth2 provider
        services.AddHttpClient<OAuth2IdentityProvider>();

        return services;
    }

    /// <summary>
    /// Decorates the IMessageBroker with authentication and authorization capabilities.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateWithSecurity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Decorate<IMessageBroker, SecurityMessageBrokerDecorator>();

        return services;
    }
}
