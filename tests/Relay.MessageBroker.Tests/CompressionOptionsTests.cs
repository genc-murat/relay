using Relay.MessageBroker.Compression;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CompressionOptionsTests
{
    [Fact]
    public void CompressionOptions_ShouldHaveCorrectDefaults()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, options.Algorithm);
        Assert.Equal(6, options.Level);
        Assert.Equal(1024, options.MinimumSizeBytes);
        Assert.True(options.AutoDetectCompressed);
        Assert.True(options.AddMetadataHeaders);
        Assert.True(options.TrackStatistics);
        Assert.Equal(0.7, options.ExpectedCompressionRatio);
        Assert.Empty(options.CompressibleContentTypes);
        Assert.NotEmpty(options.NonCompressibleContentTypes);
    }

    [Fact]
    public void CompressionOptions_NonCompressibleContentTypes_ShouldContainCommonTypes()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        Assert.Contains("image/jpeg", options.NonCompressibleContentTypes);
        Assert.Contains("image/png", options.NonCompressibleContentTypes);
        Assert.Contains("video/mp4", options.NonCompressibleContentTypes);
        Assert.Contains("application/zip", options.NonCompressibleContentTypes);
    }

    [Fact]
    public void CompressionOptions_ShouldAllowCustomization()
    {
        // Act
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli,
            Level = 9,
            MinimumSizeBytes = 2048,
            AutoDetectCompressed = false,
            AddMetadataHeaders = false,
            TrackStatistics = false,
            ExpectedCompressionRatio = 0.5
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, options.Algorithm);
        Assert.Equal(9, options.Level);
        Assert.Equal(2048, options.MinimumSizeBytes);
        Assert.False(options.AutoDetectCompressed);
        Assert.False(options.AddMetadataHeaders);
        Assert.False(options.TrackStatistics);
        Assert.Equal(0.5, options.ExpectedCompressionRatio);
    }
}