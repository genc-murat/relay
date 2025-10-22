using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

#pragma warning disable CS8625
public class PluginConfigurationTests
{
    private readonly PluginConfiguration _config;

    public PluginConfigurationTests()
    {
        _config = new PluginConfiguration();
    }

    [Fact]
    public void Indexer_Get_NonExistentKey_ReturnsNull()
    {
        // Act
        var result = _config["nonexistent"];

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Indexer_Get_ExistingKey_ReturnsValue()
    {
        // Arrange
        _config["testKey"] = "testValue";

        // Act
        var result = _config["testKey"];

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void Indexer_Set_Value_StoresValue()
    {
        // Act
        _config["testKey"] = "testValue";

        // Assert
        Assert.Equal("testValue", _config["testKey"]);
    }

    [Fact]
    public void Indexer_Set_NullValue_RemovesKey()
    {
        // Arrange
        _config["testKey"] = "testValue";

        // Act
        _config["testKey"] = null;

        // Assert
        Assert.Null(_config["testKey"]);
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsDefault()
    {
        // Act
        var result = await _config.GetAsync<string>("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsConvertedValue()
    {
        // Arrange
        _config["testKey"] = "42";

        // Act
        var result = await _config.GetAsync<int>("testKey");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetAsync_TypeConversionFailure_ReturnsDefault()
    {
        // Arrange
        _config["testKey"] = "notanumber";

        // Act
        var result = await _config.GetAsync<int>("testKey");

        // Assert
        Assert.Equal(0, result); // default for int
    }

    [Fact]
    public async Task GetAsync_BoolConversion_ReturnsCorrectValue()
    {
        // Arrange
        _config["boolKey"] = "true";

        // Act
        var result = await _config.GetAsync<bool>("boolKey");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SetAsync_Value_StoresStringRepresentation()
    {
        // Act
        await _config.SetAsync("testKey", 42);

        // Assert
        Assert.Equal("42", _config["testKey"]);
    }

    [Fact]
    public async Task SetAsync_NullValue_DoesNotStore()
    {
        // Act
        await _config.SetAsync<string>("testKey", null);

        // Assert
        Assert.Null(_config["testKey"]);
    }

    [Fact]
    public async Task ContainsKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _config.ContainsKeyAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ContainsKeyAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        _config["testKey"] = "value";

        // Act
        var result = await _config.ContainsKeyAsync("testKey");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesKey()
    {
        // Arrange
        _config["testKey"] = "value";

        // Act
        await _config.RemoveAsync("testKey");

        // Assert
        Assert.Null(_config["testKey"]);
        Assert.False(await _config.ContainsKeyAsync("testKey"));
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await _config.RemoveAsync("nonexistent");
    }

    [Fact]
    public async Task ComplexTypeConversion_FailsForUnsupportedTypes()
    {
        // Arrange - Guid conversion is not supported by Convert.ChangeType
        var guid = Guid.NewGuid();
        _config["guidKey"] = guid.ToString();

        // Act
        var result = await _config.GetAsync<Guid>("guidKey");

        // Assert - Should return default Guid since conversion fails
        Assert.Equal(Guid.Empty, result);
    }

    [Fact]
    public async Task MultipleOperations_WorkTogether()
    {
        // Act
        await _config.SetAsync("key1", "value1");
        await _config.SetAsync("key2", 123);
        await _config.SetAsync("key3", true);

        // Assert
        Assert.Equal("value1", await _config.GetAsync<string>("key1"));
        Assert.Equal(123, await _config.GetAsync<int>("key2"));
        Assert.True(await _config.GetAsync<bool>("key3"));

        Assert.True(await _config.ContainsKeyAsync("key1"));
        Assert.True(await _config.ContainsKeyAsync("key2"));
        Assert.True(await _config.ContainsKeyAsync("key3"));

        await _config.RemoveAsync("key2");
        Assert.False(await _config.ContainsKeyAsync("key2"));
        Assert.True(await _config.ContainsKeyAsync("key1"));
        Assert.True(await _config.ContainsKeyAsync("key3"));
    }
}