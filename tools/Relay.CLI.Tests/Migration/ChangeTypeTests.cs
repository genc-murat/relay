using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class ChangeTypeTests
{
    [Fact]
    public void ChangeType_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ChangeType.Add);
        Assert.Equal(1, (int)ChangeType.Remove);
        Assert.Equal(2, (int)ChangeType.Modify);
    }

    [Fact]
    public void ChangeType_CanParseAdd()
    {
        // Act
        var result = Enum.Parse<ChangeType>("Add");

        // Assert
        Assert.Equal(ChangeType.Add, result);
    }

    [Fact]
    public void ChangeType_CanParseRemove()
    {
        // Act
        var result = Enum.Parse<ChangeType>("Remove");

        // Assert
        Assert.Equal(ChangeType.Remove, result);
    }

    [Fact]
    public void ChangeType_CanParseModify()
    {
        // Act
        var result = Enum.Parse<ChangeType>("Modify");

        // Assert
        Assert.Equal(ChangeType.Modify, result);
    }

    [Fact]
    public void ChangeType_ToStringReturnsExpectedValues()
    {
        // Assert
        Assert.Equal("Add", ChangeType.Add.ToString());
        Assert.Equal("Remove", ChangeType.Remove.ToString());
        Assert.Equal("Modify", ChangeType.Modify.ToString());
    }

    [Fact]
    public void ChangeType_GetValuesReturnsAllValues()
    {
        // Act
        var values = Enum.GetValues<ChangeType>();

        // Assert
        Assert.Equal(3, values.Length);
        Assert.Contains(ChangeType.Add, values);
        Assert.Contains(ChangeType.Remove, values);
        Assert.Contains(ChangeType.Modify, values);
    }
}
