using Microsoft.CodeAnalysis;
using Moq;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Validation;
using System.Collections.Generic;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Unit tests for DiagnosticService methods.
/// Tests all diagnostic reporting and creation functionality.
/// </summary>
public class DiagnosticServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullReporter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiagnosticService(null!));
    }

    [Fact]
    public void Constructor_WithValidReporter_CreatesInstance()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();

        // Act
        var service = new DiagnosticService(reporter.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region Report Tests

    [Fact]
    public void Report_WithNullDiagnostic_ThrowsArgumentNullException()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Report(null!));
    }

    [Fact]
    public void Report_WithValidDiagnostic_ReportsToReporter()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.GeneratorError, Location.None, "Test message");

        // Act
        service.Report(diagnostic);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(diagnostic), Times.Once);
    }

    #endregion

    #region ReportValidationResult Tests

    [Fact]
    public void ReportValidationResult_WithNullValidationResult_ThrowsArgumentNullException()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ReportValidationResult(null!));
    }

    [Fact]
    public void ReportValidationResult_WithValidationResultWithErrors_ReportsErrors()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var validationResult = new ValidationResult();
        validationResult.AddError("Test error message", Location.None, DiagnosticDescriptors.GeneratorError);

        // Act
        service.ReportValidationResult(validationResult);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.GeneratorError &&
            d.GetMessage().Contains("Test error message"))), Times.Once);
    }

    [Fact]
    public void ReportValidationResult_WithValidationResultWithWarnings_ReportsWarnings()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var validationResult = new ValidationResult();
        validationResult.AddWarning("Test warning message", Location.None);

        // Act
        service.ReportValidationResult(validationResult);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.PerformanceWarning &&
            d.GetMessage().Contains("Performance") &&
            d.GetMessage().Contains("Test warning message"))), Times.Once);
    }

    [Fact]
    public void ReportValidationResult_WithValidationResultWithWarningsWithoutDescriptor_ReportsWithDefaultDescriptor()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var validationResult = new ValidationResult();
        validationResult.AddWarning("Test warning message", Location.None);

        // Act
        service.ReportValidationResult(validationResult);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(It.Is<Diagnostic>(d =>
            d.Descriptor == DiagnosticDescriptors.PerformanceWarning &&
            d.GetMessage().Contains("Performance") &&
            d.GetMessage().Contains("Test warning message"))), Times.Once);
    }

    [Fact]
    public void ReportValidationResult_WithMultipleErrorsAndWarnings_ReportsAll()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var validationResult = new ValidationResult();
        validationResult.AddError("Error 1", Location.None);
        validationResult.AddError("Error 2", Location.None);
        validationResult.AddWarning("Warning 1", Location.None);
        validationResult.AddWarning("Warning 2", Location.None);

        // Act
        service.ReportValidationResult(validationResult);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Exactly(4));
    }

    [Fact]
    public void ReportValidationResult_WithEmptyValidationResult_ReportsNothing()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var validationResult = new ValidationResult();

        // Act
        service.ReportValidationResult(validationResult);

        // Assert
        reporter.Verify(r => r.ReportDiagnostic(It.IsAny<Diagnostic>()), Times.Never);
    }

    #endregion

    #region CreateDiagnostic Tests

    [Fact]
    public void CreateDiagnostic_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.CreateDiagnostic(null!, Location.None));
    }

    [Fact]
    public void CreateDiagnostic_WithDescriptorAndLocation_CreatesDiagnostic()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);
        var location = Location.None;

        // Act
        var result = service.CreateDiagnostic(DiagnosticDescriptors.GeneratorError, location, "Test message");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DiagnosticDescriptors.GeneratorError, result.Descriptor);
        Assert.Equal(location, result.Location);
        Assert.Contains("Test message", result.GetMessage());
    }

    [Fact]
    public void CreateDiagnostic_WithNullLocation_UsesLocationNone()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act
        var result = service.CreateDiagnostic(DiagnosticDescriptors.GeneratorError, null, "Test message");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Location.None, result.Location);
    }

    [Fact]
    public void CreateDiagnostic_WithNoMessageArgs_CreatesDiagnostic()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act
        var result = service.CreateDiagnostic(DiagnosticDescriptors.GeneratorError, Location.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DiagnosticDescriptors.GeneratorError, result.Descriptor);
    }

    [Fact]
    public void CreateDiagnostic_WithMultipleMessageArgs_FormatsMessage()
    {
        // Arrange
        var reporter = new Mock<IDiagnosticReporter>();
        var service = new DiagnosticService(reporter.Object);

        // Act
        var result = service.CreateDiagnostic(DiagnosticDescriptors.InvalidHandlerSignature, Location.None, "handler", "TestHandler", "invalid signature");

        // Assert
        Assert.NotNull(result);
        var message = result.GetMessage();
        Assert.Contains("handler", message);
        Assert.Contains("TestHandler", message);
        Assert.Contains("invalid signature", message);
    }

    #endregion
}