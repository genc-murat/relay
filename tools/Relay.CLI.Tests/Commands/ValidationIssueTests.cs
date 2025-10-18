using Relay.CLI.Commands.Models.Optimization;
using System.Reflection;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class ValidationIssueTests
{
    private Type GetValidationIssueType()
    {
        // Use the same assembly that's already loaded via project reference
        var assembly = typeof(OptimizationContext).Assembly;
        return assembly.GetTypes().First(t => t.Name == "ValidationIssue");
    }

    [Fact]
    public void ValidationIssue_ShouldHaveTypeProperty()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        var typeProperty = type.GetProperty("Type");
        typeProperty!.SetValue(instance, "Handler Issue");
        var value = typeProperty.GetValue(instance);

        // Assert
        Assert.Equal("Handler Issue", value);
    }

    [Fact]
    public void ValidationIssue_ShouldHaveSeverityProperty()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        var severityProperty = type.GetProperty("Severity");
        severityProperty!.SetValue(instance, "High");
        var value = severityProperty.GetValue(instance);

        // Assert
        Assert.Equal("High", value);
    }

    [Fact]
    public void ValidationIssue_ShouldHaveMessageProperty()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        var messageProperty = type.GetProperty("Message");
        messageProperty!.SetValue(instance, "Missing CancellationToken");
        var value = messageProperty.GetValue(instance);

        // Assert
        Assert.Equal("Missing CancellationToken", value);
    }

    [Fact]
    public void ValidationIssue_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Assert
        var typeValue = type.GetProperty("Type")!.GetValue(instance);
        var severityValue = type.GetProperty("Severity")!.GetValue(instance);
        var messageValue = type.GetProperty("Message")!.GetValue(instance);

        Assert.Equal("", typeValue);
        Assert.Equal("", severityValue);
        Assert.Equal("", messageValue);
    }

    [Fact]
    public void ValidationIssue_CanSetAllProperties()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "Handler Pattern");
        type.GetProperty("Severity")!.SetValue(instance, "Medium");
        type.GetProperty("Message")!.SetValue(instance, "Handler should use ValueTask");

        // Assert
        Assert.Equal("Handler Pattern", type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal("Medium", type.GetProperty("Severity")!.GetValue(instance));
        Assert.Equal("Handler should use ValueTask", type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_TypeProperty_CanBeEmpty()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "");

        // Assert
        Assert.Equal("", type.GetProperty("Type")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_SeverityProperty_CanHaveVariousLevels()
    {
        // Arrange
        var type = GetValidationIssueType();
        var severities = new[] { "Low", "Medium", "High", "Critical" };

        foreach (var severity in severities)
        {
            // Act
            var instance = Activator.CreateInstance(type);
            type.GetProperty("Severity")!.SetValue(instance, severity);

            // Assert
            Assert.Equal(severity, type.GetProperty("Severity")!.GetValue(instance));
        }
    }

    [Fact]
    public void ValidationIssue_MessageProperty_CanContainDetailedInformation()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);
        var detailedMessage = "Handler 'CreateUserHandler' in CreateUser.cs uses Task instead of ValueTask";

        // Act
        type.GetProperty("Message")!.SetValue(instance, detailedMessage);

        // Assert
        Assert.Equal(detailedMessage, type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_ShouldBeClass()
    {
        // Arrange & Act
        var type = GetValidationIssueType();

        // Assert
        Assert.True(type.IsClass);
    }

    [Fact]
    public void ValidationIssue_ShouldBeInternal()
    {
        // Arrange & Act
        var type = GetValidationIssueType();

        // Assert
        Assert.True(type.IsNotPublic);
    }

    [Fact]
    public void ValidationIssue_ShouldHaveThreeProperties()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.Equal(3, properties.Length);
        Assert.Contains(properties, p => p.Name == "Type");
        Assert.Contains(properties, p => p.Name == "Severity");
        Assert.Contains(properties, p => p.Name == "Message");
    }

    [Fact]
    public void ValidationIssue_AllProperties_ShouldBeStrings()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.True(properties.All(p => p.PropertyType == typeof(string)));
    }

    [Fact]
    public void ValidationIssue_AllProperties_ShouldHaveGettersAndSetters()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        foreach (var property in properties)
        {
            Assert.True(property.CanRead, $"{property.Name} should have a getter");
            Assert.True(property.CanWrite, $"{property.Name} should have a setter");
        }
    }

    [Theory]
    [InlineData("Handler Pattern", "High", "Missing CancellationToken parameter")]
    [InlineData("Request Pattern", "Medium", "Request uses class instead of record")]
    [InlineData("Code Quality", "Low", "Consider using latest C# features")]
    [InlineData("Performance", "Critical", "Blocking call detected in async method")]
    public void ValidationIssue_WithVariousScenarios_ShouldStoreCorrectValues(string issueType, string severity, string message)
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, issueType);
        type.GetProperty("Severity")!.SetValue(instance, severity);
        type.GetProperty("Message")!.SetValue(instance, message);

        // Assert
        Assert.Equal(issueType, type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal(severity, type.GetProperty("Severity")!.GetValue(instance));
        Assert.Equal(message, type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_TypeProperty_CanDescribeVariousIssueTypes()
    {
        // Arrange
        var type = GetValidationIssueType();
        var issueTypes = new[]
        {
            "Handler Pattern",
            "Request Pattern",
            "Code Quality",
            "Performance",
            "Security",
            "Best Practices"
        };

        foreach (var issueType in issueTypes)
        {
            // Act
            var instance = Activator.CreateInstance(type);
            type.GetProperty("Type")!.SetValue(instance, issueType);

            // Assert
            Assert.Equal(issueType, type.GetProperty("Type")!.GetValue(instance));
        }
    }

    [Fact]
    public void ValidationIssue_SeverityProperty_ShouldIndicatePriority()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Severity")!.SetValue(instance, "Critical");

        // Assert
        Assert.Equal("Critical", type.GetProperty("Severity")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_MessageProperty_CanBeMultiline()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);
        var multilineMessage = "Handler validation failed:\n- Missing CancellationToken\n- Using Task instead of ValueTask";

        // Act
        type.GetProperty("Message")!.SetValue(instance, multilineMessage);

        // Assert
        Assert.Equal(multilineMessage, type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_CanBeUsedInList()
    {
        // Arrange
        var type = GetValidationIssueType();
        var listType = typeof(List<>).MakeGenericType(type);
        var list = Activator.CreateInstance(listType);

        var instance1 = Activator.CreateInstance(type);
        type.GetProperty("Type")!.SetValue(instance1, "Issue 1");
        type.GetProperty("Severity")!.SetValue(instance1, "High");
        type.GetProperty("Message")!.SetValue(instance1, "First issue");

        var instance2 = Activator.CreateInstance(type);
        type.GetProperty("Type")!.SetValue(instance2, "Issue 2");
        type.GetProperty("Severity")!.SetValue(instance2, "Medium");
        type.GetProperty("Message")!.SetValue(instance2, "Second issue");

        // Act
        var addMethod = listType.GetMethod("Add");
        addMethod!.Invoke(list, new[] { instance1 });
        addMethod!.Invoke(list, new[] { instance2 });

        var countProperty = listType.GetProperty("Count");
        var count = countProperty!.GetValue(list);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void ValidationIssue_WithHandlerPatternIssue_ShouldStoreCorrectly()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "Handler Pattern");
        type.GetProperty("Severity")!.SetValue(instance, "High");
        type.GetProperty("Message")!.SetValue(instance, "CreateUserHandler uses Task instead of ValueTask");

        // Assert
        Assert.Equal("Handler Pattern", type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal("High", type.GetProperty("Severity")!.GetValue(instance));
        Assert.Contains("ValueTask", (string)type.GetProperty("Message")!.GetValue(instance)!);
    }

    [Fact]
    public void ValidationIssue_WithRequestPatternIssue_ShouldStoreCorrectly()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "Request Pattern");
        type.GetProperty("Severity")!.SetValue(instance, "Medium");
        type.GetProperty("Message")!.SetValue(instance, "CreateUserRequest uses class instead of record");

        // Assert
        Assert.Equal("Request Pattern", type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal("Medium", type.GetProperty("Severity")!.GetValue(instance));
        Assert.Contains("record", (string)type.GetProperty("Message")!.GetValue(instance)!);
    }

    [Fact]
    public void ValidationIssue_WithCodeQualityIssue_ShouldStoreCorrectly()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "Code Quality");
        type.GetProperty("Severity")!.SetValue(instance, "Low");
        type.GetProperty("Message")!.SetValue(instance, "Consider enabling nullable reference types");

        // Assert
        Assert.Equal("Code Quality", type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal("Low", type.GetProperty("Severity")!.GetValue(instance));
        Assert.Contains("nullable", (string)type.GetProperty("Message")!.GetValue(instance)!);
    }

    [Fact]
    public void ValidationIssue_PropertiesCanBeModified()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act - Initial values
        type.GetProperty("Type")!.SetValue(instance, "Initial Type");
        type.GetProperty("Severity")!.SetValue(instance, "Low");
        type.GetProperty("Message")!.SetValue(instance, "Initial message");

        // Act - Modify values
        type.GetProperty("Type")!.SetValue(instance, "Modified Type");
        type.GetProperty("Severity")!.SetValue(instance, "Critical");
        type.GetProperty("Message")!.SetValue(instance, "Modified message");

        // Assert
        Assert.Equal("Modified Type", type.GetProperty("Type")!.GetValue(instance));
        Assert.Equal("Critical", type.GetProperty("Severity")!.GetValue(instance));
        Assert.Equal("Modified message", type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_MessageCanContainSpecialCharacters()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);
        var messageWithSpecialChars = "Handler 'CreateUser' -> missing [Handle] attribute";

        // Act
        type.GetProperty("Message")!.SetValue(instance, messageWithSpecialChars);

        // Assert
        Assert.Equal(messageWithSpecialChars, type.GetProperty("Message")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_TypeCanContainSpaces()
    {
        // Arrange
        var type = GetValidationIssueType();
        var instance = Activator.CreateInstance(type);

        // Act
        type.GetProperty("Type")!.SetValue(instance, "Handler Pattern Violation");

        // Assert
        Assert.Equal("Handler Pattern Violation", type.GetProperty("Type")!.GetValue(instance));
    }

    [Fact]
    public void ValidationIssue_SeverityLevels_ShouldBeConsistent()
    {
        // Arrange
        var type = GetValidationIssueType();
        var standardSeverities = new[] { "Info", "Low", "Medium", "High", "Critical" };

        // Act & Assert - All standard severities should be storable
        foreach (var severity in standardSeverities)
        {
            var instance = Activator.CreateInstance(type);
            type.GetProperty("Severity")!.SetValue(instance, severity);
            Assert.Equal(severity, type.GetProperty("Severity")!.GetValue(instance));
        }
    }
}
