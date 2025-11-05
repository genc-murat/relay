namespace Relay.Core.AI
{
    /// <summary>
    /// Cache key generation strategies.
    /// </summary>
    public enum CacheKeyStrategy
    {
        FullRequest,
        RequestTypeOnly,
        SelectedProperties,
        Custom
    }
}