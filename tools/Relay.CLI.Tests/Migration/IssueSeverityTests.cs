using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class IssueSeverityTests
{
    [Fact]
    public void IssueSeverity_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)IssueSeverity.Info);
        Assert.Equal(1, (int)IssueSeverity.Warning);
        Assert.Equal(2, (int)IssueSeverity.Error);
        Assert.Equal(3, (int)IssueSeverity.Critical);
    }

    [Fact]
    public void IssueSeverity_CanParseInfo()
    {
        // Act
        var result = Enum.Parse<IssueSeverity>("Info");

        // Assert
        Assert.Equal(IssueSeverity.Info, result);
    }

    [Fact]
    public void IssueSeverity_CanParseWarning()
    {
        // Act
        var result = Enum.Parse<IssueSeverity>("Warning");

        // Assert
        Assert.Equal(IssueSeverity.Warning, result);
    }

    [Fact]
    public void IssueSeverity_CanParseError()
    {
        // Act
        var result = Enum.Parse<IssueSeverity>("Error");

        // Assert
        Assert.Equal(IssueSeverity.Error, result);
    }

    [Fact]
    public void IssueSeverity_CanParseCritical()
    {
        // Act
        var result = Enum.Parse<IssueSeverity>("Critical");

        // Assert
        Assert.Equal(IssueSeverity.Critical, result);
    }

    [Fact]
    public void IssueSeverity_ToStringReturnsExpectedValues()
    {
        // Assert
        Assert.Equal("Info", IssueSeverity.Info.ToString());
        Assert.Equal("Warning", IssueSeverity.Warning.ToString());
        Assert.Equal("Error", IssueSeverity.Error.ToString());
        Assert.Equal("Critical", IssueSeverity.Critical.ToString());
    }

    [Fact]
    public void IssueSeverity_GetValuesReturnsAllValues()
    {
        // Act
        var values = Enum.GetValues<IssueSeverity>();

        // Assert
        Assert.Equal(4, values.Length);
        Assert.Contains(IssueSeverity.Info, values);
        Assert.Contains(IssueSeverity.Warning, values);
        Assert.Contains(IssueSeverity.Error, values);
        Assert.Contains(IssueSeverity.Critical, values);
    }
}