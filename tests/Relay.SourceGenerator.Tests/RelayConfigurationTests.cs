using Relay.SourceGenerator.Configuration;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for RelayConfiguration value-based equality and caching behavior.
/// </summary>
public class RelayConfigurationTests
{
    [Fact]
    public void RelayConfiguration_DefaultInstance_HasDefaultOptions()
    {
        // Arrange & Act
        var config = RelayConfiguration.Default;

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Options);
        Assert.True(config.Options.EnableDIGeneration);
        Assert.True(config.Options.EnableOptimizedDispatcher);
    }

    [Fact]
    public void RelayConfiguration_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var options1 = new GenerationOptions
        {
            IncludeDebugInfo = true,
            EnableNullableContext = true,
            CustomNamespace = "Test.Namespace",
            MaxDegreeOfParallelism = 4
        };

        var options2 = new GenerationOptions
        {
            IncludeDebugInfo = true,
            EnableNullableContext = true,
            CustomNamespace = "Test.Namespace",
            MaxDegreeOfParallelism = 4
        };

        var config1 = new RelayConfiguration { Options = options1 };
        var config2 = new RelayConfiguration { Options = options2 };

        // Act
        var result = config1.Equals(config2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RelayConfiguration_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var options1 = new GenerationOptions
        {
            IncludeDebugInfo = true,
            EnableNullableContext = true
        };

        var options2 = new GenerationOptions
        {
            IncludeDebugInfo = false,
            EnableNullableContext = true
        };

        var config1 = new RelayConfiguration { Options = options1 };
        var config2 = new RelayConfiguration { Options = options2 };

        // Act
        var result = config1.Equals(config2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RelayConfiguration_GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var options1 = new GenerationOptions
        {
            IncludeDebugInfo = true,
            CustomNamespace = "Test.Namespace",
            MaxDegreeOfParallelism = 8
        };

        var options2 = new GenerationOptions
        {
            IncludeDebugInfo = true,
            CustomNamespace = "Test.Namespace",
            MaxDegreeOfParallelism = 8
        };

        var config1 = new RelayConfiguration { Options = options1 };
        var config2 = new RelayConfiguration { Options = options2 };

        // Act
        var hash1 = config1.GetHashCode();
        var hash2 = config2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void RelayConfiguration_GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        // Arrange
        var options1 = new GenerationOptions
        {
            MaxDegreeOfParallelism = 4
        };

        var options2 = new GenerationOptions
        {
            MaxDegreeOfParallelism = 8
        };

        var config1 = new RelayConfiguration { Options = options1 };
        var config2 = new RelayConfiguration { Options = options2 };

        // Act
        var hash1 = config1.GetHashCode();
        var hash2 = config2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void RelayConfiguration_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var config = new RelayConfiguration();

        // Act
        var result = config.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RelayConfiguration_Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var config = new RelayConfiguration();

        // Act
        var result = config.Equals(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RelayConfigurationComparer_Instance_IsNotNull()
    {
        // Act
        var comparer = RelayConfigurationComparer.Instance;

        // Assert
        Assert.NotNull(comparer);
    }

    [Fact]
    public void RelayConfigurationComparer_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var comparer = RelayConfigurationComparer.Instance;
        var options = new GenerationOptions { IncludeDebugInfo = true };
        var config1 = new RelayConfiguration { Options = options };
        var config2 = new RelayConfiguration { Options = options };

        // Act
        var result = comparer.Equals(config1, config2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RelayConfigurationComparer_Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        var comparer = RelayConfigurationComparer.Instance;

        // Act
        var result = comparer.Equals(null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RelayConfigurationComparer_Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        var comparer = RelayConfigurationComparer.Instance;
        var config = new RelayConfiguration();

        // Act
        var result = comparer.Equals(config, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RelayConfigurationComparer_GetHashCode_ReturnsConfigHashCode()
    {
        // Arrange
        var comparer = RelayConfigurationComparer.Instance;
        var config = new RelayConfiguration();

        // Act
        var comparerHash = comparer.GetHashCode(config);
        var configHash = config.GetHashCode();

        // Assert
        Assert.Equal(configHash, comparerHash);
    }

    [Fact]
    public void RelayConfigurationComparer_GetHashCode_NullConfig_ReturnsZero()
    {
        // Arrange
        var comparer = RelayConfigurationComparer.Instance;

        // Act
        var hash = comparer.GetHashCode(null!);

        // Assert
        Assert.Equal(0, hash);
    }
}
