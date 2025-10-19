namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for objects that can estimate their serialized size.
    /// </summary>
    public interface IEstimateSize
    {
        /// <summary>
        /// Estimates the size of the object in bytes.
        /// </summary>
        long EstimateSize();
    }
}
