using System;
using Xunit;
using Relay.Core.Diagnostics.Validation;

namespace Relay.Core.Tests.Diagnostics
{
    public class ValidationTests
    {
        public class ValidationSeverityTests
        {
            [Fact]
            public void ValidationSeverity_ShouldHaveExpectedValues()
            {
                // Assert
                var expectedValues = new[] { ValidationSeverity.Info, ValidationSeverity.Warning, ValidationSeverity.Error };
                var actualValues = Enum.GetValues<ValidationSeverity>();
                foreach (var value in expectedValues)
                {
                    Assert.Contains(value, actualValues);
                }
            }

            [Fact]
            public void ValidationSeverity_InfoShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(0, (int)ValidationSeverity.Info);
            }

            [Fact]
            public void ValidationSeverity_WarningShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(1, (int)ValidationSeverity.Warning);
            }

            [Fact]
            public void ValidationSeverity_ErrorShouldHaveCorrectValue()
            {
                // Assert
                Assert.Equal(2, (int)ValidationSeverity.Error);
            }

            [Fact]
            public void ValidationSeverity_AllValues_ShouldBeParseable()
            {
                // Assert
                Assert.True(Enum.TryParse("Info", out ValidationSeverity info));
                Assert.Equal(ValidationSeverity.Info, info);

                Assert.True(Enum.TryParse("Warning", out ValidationSeverity warning));
                Assert.Equal(ValidationSeverity.Warning, warning);

                Assert.True(Enum.TryParse("Error", out ValidationSeverity error));
                Assert.Equal(ValidationSeverity.Error, error);
            }
        }

        public class ValidationIssueTests
        {
            [Fact]
            public void ValidationIssue_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var issue = new ValidationIssue();

                // Assert
                Assert.Equal(default(ValidationSeverity), issue.Severity);
                Assert.Equal(string.Empty, issue.Message);
                Assert.Equal(string.Empty, issue.Category);
                Assert.Null(issue.Code);
            }

            [Fact]
            public void ValidationIssue_ShouldAllowSettingProperties()
            {
                // Arrange
                var issue = new ValidationIssue();

                // Act
                issue.Severity = ValidationSeverity.Error;
                issue.Message = "Test message";
                issue.Category = "TestCategory";
                issue.Code = "TEST001";

                // Assert
                Assert.Equal(ValidationSeverity.Error, issue.Severity);
                Assert.Equal("Test message", issue.Message);
                Assert.Equal("TestCategory", issue.Category);
                Assert.Equal("TEST001", issue.Code);
            }

            [Fact]
            public void ValidationIssue_ShouldBeMutable()
            {
                // Arrange
                var issue = new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message = "Initial message",
                    Category = "Initial",
                    Code = "INIT"
                };

                // Act
                issue.Severity = ValidationSeverity.Error;
                issue.Message = "Updated message";
                issue.Category = "Updated";
                issue.Code = "UPD";

                // Assert
                Assert.Equal(ValidationSeverity.Error, issue.Severity);
                Assert.Equal("Updated message", issue.Message);
                Assert.Equal("Updated", issue.Category);
                Assert.Equal("UPD", issue.Code);
            }

            [Fact]
            public void ValidationIssue_CanBeUsedInCollections()
            {
                // Arrange
                var issues = new System.Collections.Generic.List<ValidationIssue>
                {
                    new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Message = "Error 1",
                        Category = "Cat1",
                        Code = "E001"
                    },
                    new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = "Warning 1",
                        Category = "Cat2",
                        Code = "W001"
                    }
                };

                // Assert
                Assert.Equal(2, issues.Count);
                Assert.Equal(ValidationSeverity.Error, issues[0].Severity);
                Assert.Equal("Error 1", issues[0].Message);
                Assert.Equal("Cat1", issues[0].Category);
                Assert.Equal("E001", issues[0].Code);

                Assert.Equal(ValidationSeverity.Warning, issues[1].Severity);
                Assert.Equal("Warning 1", issues[1].Message);
                Assert.Equal("Cat2", issues[1].Category);
                Assert.Equal("W001", issues[1].Code);
            }
        }

        public class ValidationResultTests
        {
            [Fact]
            public void ValidationResult_DefaultConstructor_ShouldInitializeProperties()
            {
                // Arrange & Act
                var result = new ValidationResult();

                // Assert
                Assert.True(result.IsValid);
                Assert.Empty(result.Issues);
                Assert.Equal("Configuration is valid", result.Summary);
                Assert.Equal(0, result.ErrorCount);
                Assert.Equal(0, result.WarningCount);
                Assert.Equal(0, result.InfoCount);
            }

            [Fact]
            public void ValidationResult_IsValid_ShouldBeTrue_WhenNoErrors()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddWarning("Test warning");
                result.AddInfo("Test info");

                // Assert
                Assert.True(result.IsValid);
                Assert.Equal(0, result.ErrorCount);
                Assert.Equal(1, result.WarningCount);
                Assert.Equal(1, result.InfoCount);
            }

            [Fact]
            public void ValidationResult_IsValid_ShouldBeFalse_WhenHasErrors()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddError("Test error");
                result.AddWarning("Test warning");

                // Assert
                Assert.False(result.IsValid);
                Assert.Equal(1, result.ErrorCount);
                Assert.Equal(1, result.WarningCount);
                Assert.Equal(0, result.InfoCount);
            }

            [Fact]
            public void ValidationResult_Summary_ShouldReflectValidity()
            {
                // Arrange
                var validResult = new ValidationResult();
                var invalidResult = new ValidationResult();

                // Act
                invalidResult.AddError("Error 1");
                invalidResult.AddError("Error 2");
                invalidResult.AddWarning("Warning 1");

                // Assert
                Assert.Equal("Configuration is valid", validResult.Summary);
                Assert.Equal("Found 2 errors and 1 warnings", invalidResult.Summary);
            }

            [Fact]
            public void ValidationResult_AddIssue_ShouldAddIssueWithDefaultCategory()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddIssue(ValidationSeverity.Error, "Test message");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
                Assert.Equal("Test message", result.Issues[0].Message);
                Assert.Equal("General", result.Issues[0].Category);
                Assert.Null(result.Issues[0].Code);
            }

            [Fact]
            public void ValidationResult_AddIssue_ShouldAddIssueWithCustomCategory()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddIssue(ValidationSeverity.Warning, "Test message", "CustomCategory");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Warning, result.Issues[0].Severity);
                Assert.Equal("Test message", result.Issues[0].Message);
                Assert.Equal("CustomCategory", result.Issues[0].Category);
                Assert.Null(result.Issues[0].Code);
            }

            [Fact]
            public void ValidationResult_AddError_ShouldAddErrorIssue()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddError("Test error");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
                Assert.Equal("Test error", result.Issues[0].Message);
                Assert.Equal("General", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_AddError_ShouldAddErrorIssueWithCustomCategory()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddError("Test error", "Custom");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
                Assert.Equal("Test error", result.Issues[0].Message);
                Assert.Equal("Custom", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_AddWarning_ShouldAddWarningIssue()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddWarning("Test warning");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Warning, result.Issues[0].Severity);
                Assert.Equal("Test warning", result.Issues[0].Message);
                Assert.Equal("General", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_AddWarning_ShouldAddWarningIssueWithCustomCategory()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddWarning("Test warning", "Custom");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Warning, result.Issues[0].Severity);
                Assert.Equal("Test warning", result.Issues[0].Message);
                Assert.Equal("Custom", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_AddInfo_ShouldAddInfoIssue()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddInfo("Test info");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Info, result.Issues[0].Severity);
                Assert.Equal("Test info", result.Issues[0].Message);
                Assert.Equal("General", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_AddInfo_ShouldAddInfoIssueWithCustomCategory()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddInfo("Test info", "Custom");

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(ValidationSeverity.Info, result.Issues[0].Severity);
                Assert.Equal("Test info", result.Issues[0].Message);
                Assert.Equal("Custom", result.Issues[0].Category);
            }

            [Fact]
            public void ValidationResult_ShouldHandleMultipleIssues()
            {
                // Arrange
                var result = new ValidationResult();

                // Act
                result.AddError("Error 1", "Cat1");
                result.AddWarning("Warning 1", "Cat2");
                result.AddInfo("Info 1", "Cat3");
                result.AddError("Error 2", "Cat1");

                // Assert
                Assert.Equal(4, result.Issues.Count);
                Assert.False(result.IsValid);
                Assert.Equal(2, result.ErrorCount);
                Assert.Equal(1, result.WarningCount);
                Assert.Equal(1, result.InfoCount);
                Assert.Equal("Found 2 errors and 1 warnings", result.Summary);
            }

            [Fact]
            public void ValidationResult_Issues_ShouldBeMutableList()
            {
                // Arrange
                var result = new ValidationResult();
                var issue = new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Test",
                    Category = "Test"
                };

                // Act
                result.Issues.Add(issue);

                // Assert
                Assert.Single(result.Issues);
                Assert.Equal(issue, result.Issues[0]);
                Assert.False(result.IsValid);
            }
        }
    }
}