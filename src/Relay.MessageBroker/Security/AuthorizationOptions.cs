namespace Relay.MessageBroker.Security;

/// <summary>
/// Configuration options for message authorization.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    /// Gets or sets the role-to-permission mappings for publish operations.
    /// Key: role name, Value: list of allowed message types or topics.
    /// </summary>
    public Dictionary<string, List<string>> PublishPermissions { get; set; } = new();

    /// <summary>
    /// Gets or sets the role-to-permission mappings for subscribe operations.
    /// Key: role name, Value: list of allowed message types or topics.
    /// </summary>
    public Dictionary<string, List<string>> SubscribePermissions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to allow all operations when no specific permissions are defined.
    /// Default is false (deny by default).
    /// </summary>
    public bool AllowByDefault { get; set; } = false;

    /// <summary>
    /// Gets or sets the claim type used to extract roles from JWT tokens.
    /// Default is "role".
    /// </summary>
    public string RoleClaimType { get; set; } = "role";

    /// <summary>
    /// Validates the authorization options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RoleClaimType))
        {
            throw new InvalidOperationException("RoleClaimType must be specified.");
        }
    }
}
