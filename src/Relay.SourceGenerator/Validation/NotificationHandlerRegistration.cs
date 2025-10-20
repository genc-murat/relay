using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator;

/// <summary>
/// Represents a notification handler registration for validation.
/// </summary>
public class NotificationHandlerRegistration
{
    public ITypeSymbol NotificationType { get; set; } = null!;
    public IMethodSymbol Method { get; set; } = null!;
    public int Priority { get; set; }
    public Location Location { get; set; } = null!;
    public AttributeData? Attribute { get; set; }
}
