namespace Relay.MessageBroker.Security;

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
