using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationExceptionTests
{
    [Fact]
    public void MigrationException_ConstructorWithMessage_SetsMessage()
    {
        // Arrange
        var message = "Migration failed";

        // Act
        var exception = new MigrationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.FilePath);
        Assert.Null(exception.Stage);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MigrationException_ConstructorWithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Migration failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new MigrationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Null(exception.FilePath);
        Assert.Null(exception.Stage);
    }

    [Fact]
    public void MigrationException_ConstructorWithMessageAndFilePath_SetsProperties()
    {
        // Arrange
        var message = "File processing failed";
        var filePath = "/src/Handler.cs";

        // Act
        var exception = new MigrationException(message, filePath);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Null(exception.Stage);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MigrationException_ConstructorWithMessageFilePathAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "File processing failed";
        var filePath = "/src/Handler.cs";
        var innerException = new IOException("IO error");

        // Act
        var exception = new MigrationException(message, filePath, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Null(exception.Stage);
    }

    [Fact]
    public void MigrationException_ConstructorWithMessageStageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Stage failed";
        var stage = MigrationStage.TransformingCode;
        var innerException = new Exception("Transform error");

        // Act
        var exception = new MigrationException(message, stage, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(stage, exception.Stage);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Null(exception.FilePath);
    }

    [Fact]
    public void MigrationException_ConstructorWithMessageFilePathStageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Complete failure";
        var filePath = "/src/Program.cs";
        var stage = MigrationStage.Finalizing;
        var innerException = new Exception("Finalization error");

        // Act
        var exception = new MigrationException(message, filePath, stage, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Equal(stage, exception.Stage);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void MigrationException_InheritsFromException()
    {
        // Arrange & Act
        var exception = new MigrationException("Test");

        // Assert
        Assert.IsType<Exception>(exception, exactMatch: false);
    }

    [Fact]
    public void SyntaxException_ConstructorWithMessage_SetsMessage()
    {
        // Arrange
        var message = "Syntax error";

        // Act
        var exception = new SyntaxException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.LineNumber);
        Assert.Null(exception.FilePath);
    }

    [Fact]
    public void SyntaxException_ConstructorWithMessageAndFilePathAndLineNumber_SetsProperties()
    {
        // Arrange
        var message = "Invalid syntax";
        var filePath = "/src/Code.cs";
        var lineNumber = 42;

        // Act
        var exception = new SyntaxException(message, filePath, lineNumber);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Equal(lineNumber, exception.LineNumber);
    }

    [Fact]
    public void SyntaxException_ConstructorWithMessageFilePathAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Syntax parsing failed";
        var filePath = "/src/Broken.cs";
        var innerException = new FormatException("Bad format");

        // Act
        var exception = new SyntaxException(message, filePath, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(filePath, exception.FilePath);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Null(exception.LineNumber);
    }

    [Fact]
    public void SyntaxException_InheritsFromMigrationException()
    {
        // Arrange & Act
        var exception = new SyntaxException("Test");

        // Assert
        Assert.IsType<MigrationException>(exception, exactMatch: false);
    }
}
