using Relay.CLI.Commands.Models.Optimization;
using System.Reflection;

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
        value.Should().Be("Handler Issue");
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
        value.Should().Be("High");
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
        value.Should().Be("Missing CancellationToken");
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

        typeValue.Should().Be("");
        severityValue.Should().Be("");
        messageValue.Should().Be("");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Handler Pattern");
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("Medium");
        type.GetProperty("Message")!.GetValue(instance).Should().Be("Handler should use ValueTask");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("");
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
            type.GetProperty("Severity")!.GetValue(instance).Should().Be(severity);
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
        type.GetProperty("Message")!.GetValue(instance).Should().Be(detailedMessage);
    }

    [Fact]
    public void ValidationIssue_ShouldBeClass()
    {
        // Arrange & Act
        var type = GetValidationIssueType();

        // Assert
        type.IsClass.Should().BeTrue();
    }

    [Fact]
    public void ValidationIssue_ShouldBeInternal()
    {
        // Arrange & Act
        var type = GetValidationIssueType();

        // Assert
        type.IsNotPublic.Should().BeTrue();
    }

    [Fact]
    public void ValidationIssue_ShouldHaveThreeProperties()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        properties.Should().HaveCount(3);
        properties.Should().Contain(p => p.Name == "Type");
        properties.Should().Contain(p => p.Name == "Severity");
        properties.Should().Contain(p => p.Name == "Message");
    }

    [Fact]
    public void ValidationIssue_AllProperties_ShouldBeStrings()
    {
        // Arrange & Act
        var type = GetValidationIssueType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        properties.All(p => p.PropertyType == typeof(string)).Should().BeTrue();
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
            property.CanRead.Should().BeTrue($"{property.Name} should have a getter");
            property.CanWrite.Should().BeTrue($"{property.Name} should have a setter");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be(issueType);
        type.GetProperty("Severity")!.GetValue(instance).Should().Be(severity);
        type.GetProperty("Message")!.GetValue(instance).Should().Be(message);
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
            type.GetProperty("Type")!.GetValue(instance).Should().Be(issueType);
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
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("Critical");
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
        type.GetProperty("Message")!.GetValue(instance).Should().Be(multilineMessage);
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
        count.Should().Be(2);
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Handler Pattern");
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("High");
        ((string)type.GetProperty("Message")!.GetValue(instance)!).Should().Contain("ValueTask");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Request Pattern");
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("Medium");
        ((string)type.GetProperty("Message")!.GetValue(instance)!).Should().Contain("record");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Code Quality");
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("Low");
        ((string)type.GetProperty("Message")!.GetValue(instance)!).Should().Contain("nullable");
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Modified Type");
        type.GetProperty("Severity")!.GetValue(instance).Should().Be("Critical");
        type.GetProperty("Message")!.GetValue(instance).Should().Be("Modified message");
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
        type.GetProperty("Message")!.GetValue(instance).Should().Be(messageWithSpecialChars);
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
        type.GetProperty("Type")!.GetValue(instance).Should().Be("Handler Pattern Violation");
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
            type.GetProperty("Severity")!.GetValue(instance).Should().Be(severity);
        }
    }
}
