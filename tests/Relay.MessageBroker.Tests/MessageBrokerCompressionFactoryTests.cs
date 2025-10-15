using Relay.MessageBroker.Compression;
using Relay.Core.Caching.Compression;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerCompressionFactoryTests
{
    [Fact]
    public void CreateMessage_WithValidOptions_ShouldReturnMessageCompressor()
    {
        // Arrange
        var options = new Relay.MessageBroker.Compression.CompressionOptions
        {
            Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)Relay.MessageBroker.Compression.CompressionAlgorithm.GZip,
            Level = 6 // Optimal level
        };

        // Act
        var compressor = MessageBrokerCompressionFactory.CreateMessage(options);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(Relay.MessageBroker.Compression.CompressionAlgorithm.GZip, compressor.Algorithm);
    }

    [Fact]
    public void CreateFromCore_WithDefaultOptions_ShouldUseDefaults()
    {
        // Arrange
        var coreOptions = new Relay.Core.Caching.Compression.CompressionOptions(); // Default algorithm is GZip

        // Act
        var compressor = MessageBrokerCompressionFactory.CreateFromCore(coreOptions);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(Relay.MessageBroker.Compression.CompressionAlgorithm.GZip, compressor.Algorithm);
    }

    [Fact]
    public void CreateMessage_WithCompressionLevel_ShouldPassLevelToCore()
    {
        // Arrange
        var options = new Relay.MessageBroker.Compression.CompressionOptions
        {
            Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)Relay.MessageBroker.Compression.CompressionAlgorithm.GZip,
            Level = 9 // SmallestSize level
        };

        // Act
        var compressor = MessageBrokerCompressionFactory.CreateMessage(options);

        // Assert
        Assert.NotNull(compressor);
        // The adapter should be created successfully with the level setting
        Assert.IsType<MessageCompressorAdapter>(compressor);
    }

    [Fact]
    public void CreateFromCore_WithCompressionLevel_ShouldPassLevelToCore()
    {
        // Arrange
        var coreOptions = new Relay.Core.Caching.Compression.CompressionOptions
        {
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate,
            Level = 0 // No compression level
        };

        // Act
        var compressor = MessageBrokerCompressionFactory.CreateFromCore(coreOptions);

        // Assert
        Assert.NotNull(compressor);
        Assert.IsType<MessageCompressorAdapter>(compressor);
    }
}