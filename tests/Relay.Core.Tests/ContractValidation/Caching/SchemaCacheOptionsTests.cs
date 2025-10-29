using System;
using System.Collections.Generic;
using Relay.Core.ContractValidation.Caching;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Caching;

public class SchemaCacheOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new SchemaCacheOptions();

        // Assert
        Assert.Equal(1000, options.MaxCacheSize);
        Assert.False(options.EnableCacheWarming);
        Assert.NotNull(options.WarmupTypes);
        Assert.Empty(options.WarmupTypes);
        Assert.True(options.EnableMetrics);
        Assert.Equal(TimeSpan.FromMinutes(5), options.MetricsReportingInterval);
    }

    [Fact]
    public void MaxCacheSize_CanBeSet()
    {
        // Arrange
        var options = new SchemaCacheOptions();

        // Act
        options.MaxCacheSize = 500;

        // Assert
        Assert.Equal(500, options.MaxCacheSize);
    }

    [Fact]
    public void EnableCacheWarming_CanBeSet()
    {
        // Arrange
        var options = new SchemaCacheOptions();

        // Act
        options.EnableCacheWarming = true;

        // Assert
        Assert.True(options.EnableCacheWarming);
    }

    [Fact]
    public void WarmupTypes_CanBePopulated()
    {
        // Arrange
        var options = new SchemaCacheOptions();

        // Act
        options.WarmupTypes.Add(typeof(string));
        options.WarmupTypes.Add(typeof(int));

        // Assert
        Assert.Equal(2, options.WarmupTypes.Count);
        Assert.Contains(typeof(string), options.WarmupTypes);
        Assert.Contains(typeof(int), options.WarmupTypes);
    }

    [Fact]
    public void EnableMetrics_CanBeSet()
    {
        // Arrange
        var options = new SchemaCacheOptions();

        // Act
        options.EnableMetrics = false;

        // Assert
        Assert.False(options.EnableMetrics);
    }

    [Fact]
    public void MetricsReportingInterval_CanBeSet()
    {
        // Arrange
        var options = new SchemaCacheOptions();

        // Act
        options.MetricsReportingInterval = TimeSpan.FromMinutes(10);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), options.MetricsReportingInterval);
    }
}
