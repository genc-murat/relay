using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class TransformationResultTests
{
    [Fact]
    public void TransformationResult_HasDefaultValues()
    {
        // Arrange & Act
        var result = new TransformationResult();

        // Assert
        Assert.Equal("", result.FilePath);
        Assert.Equal("", result.OriginalContent);
        Assert.Equal("", result.NewContent);
        Assert.False(result.WasModified);
        Assert.Equal(0, result.LinesChanged);
        Assert.False(result.IsHandler);
        Assert.NotNull(result.Changes);
        Assert.Empty(result.Changes);
        Assert.Null(result.Error);
    }

    [Fact]
    public void TransformationResult_CanSetFilePath()
    {
        // Arrange
        var result = new TransformationResult();

        // Act
        result.FilePath = "/src/Handler.cs";

        // Assert
        Assert.Equal("/src/Handler.cs", result.FilePath);
    }

    [Fact]
    public void TransformationResult_CanSetOriginalContent()
    {
        // Arrange
        var result = new TransformationResult();
        var content = "using MediatR;\npublic class Handler { }";

        // Act
        result.OriginalContent = content;

        // Assert
        Assert.Equal(content, result.OriginalContent);
    }

    [Fact]
    public void TransformationResult_CanSetNewContent()
    {
        // Arrange
        var result = new TransformationResult();
        var content = "using Relay;\npublic class Handler { }";

        // Act
        result.NewContent = content;

        // Assert
        Assert.Equal(content, result.NewContent);
    }

    [Fact]
    public void TransformationResult_CanSetWasModified()
    {
        // Arrange
        var result = new TransformationResult();

        // Act
        result.WasModified = true;

        // Assert
        Assert.True(result.WasModified);
    }

    [Fact]
    public void TransformationResult_CanSetLinesChanged()
    {
        // Arrange
        var result = new TransformationResult();

        // Act
        result.LinesChanged = 5;

        // Assert
        Assert.Equal(5, result.LinesChanged);
    }

    [Fact]
    public void TransformationResult_CanSetIsHandler()
    {
        // Arrange
        var result = new TransformationResult();

        // Act
        result.IsHandler = true;

        // Assert
        Assert.True(result.IsHandler);
    }

    [Fact]
    public void TransformationResult_CanSetError()
    {
        // Arrange
        var result = new TransformationResult();

        // Act
        result.Error = "Syntax error on line 10";

        // Assert
        Assert.Equal("Syntax error on line 10", result.Error);
    }

    [Fact]
    public void TransformationResult_CanAddChanges()
    {
        // Arrange
        var result = new TransformationResult();
        var change = new MigrationChange
        {
            Category = "Using Directives",
            Type = ChangeType.Modify,
            Description = "Updated MediatR to Relay",
            FilePath = "/src/Handler.cs"
        };

        // Act
        result.Changes.Add(change);

        // Assert
        Assert.Single(result.Changes);
        Assert.Equal("Using Directives", result.Changes[0].Category);
        Assert.Equal(ChangeType.Modify, result.Changes[0].Type);
    }

    [Fact]
    public void TransformationResult_SupportsObjectInitializer()
    {
        // Arrange & Act
        var result = new TransformationResult
        {
            FilePath = "/src/UserHandler.cs",
            OriginalContent = "using MediatR;\nclass Handler {}",
            NewContent = "using Relay;\nclass Handler {}",
            WasModified = true,
            LinesChanged = 1,
            IsHandler = true,
            Error = null
        };

        // Assert
        Assert.Equal("/src/UserHandler.cs", result.FilePath);
        Assert.Equal("using MediatR;\nclass Handler {}", result.OriginalContent);
        Assert.Equal("using Relay;\nclass Handler {}", result.NewContent);
        Assert.True(result.WasModified);
        Assert.Equal(1, result.LinesChanged);
        Assert.True(result.IsHandler);
        Assert.Null(result.Error);
    }

    [Fact]
    public void TransformationResult_CanCreateSuccessfulTransformation()
    {
        // Arrange & Act
        var result = new TransformationResult
        {
            FilePath = "/src/Handler.cs",
            OriginalContent = "using MediatR;\npublic class MyHandler : IRequestHandler<Request, Response> {}",
            NewContent = "using Relay;\npublic class MyHandler : IRequestHandler<Request, Response> {}",
            WasModified = true,
            LinesChanged = 1,
            IsHandler = true
        };

        // Assert
        Assert.Equal("/src/Handler.cs", result.FilePath);
        Assert.True(result.WasModified);
        Assert.Equal(1, result.LinesChanged);
        Assert.True(result.IsHandler);
        Assert.Null(result.Error);
    }

    [Fact]
    public void TransformationResult_CanCreateFailedTransformation()
    {
        // Arrange & Act
        var result = new TransformationResult
        {
            FilePath = "/src/BrokenHandler.cs",
            OriginalContent = "using MediatR;\npublic class BrokenHandler {",
            NewContent = "",
            WasModified = false,
            LinesChanged = 0,
            IsHandler = false,
            Error = "Syntax error: missing closing brace"
        };

        // Assert
        Assert.Equal("/src/BrokenHandler.cs", result.FilePath);
        Assert.False(result.WasModified);
        Assert.Equal(0, result.LinesChanged);
        Assert.False(result.IsHandler);
        Assert.Equal("Syntax error: missing closing brace", result.Error);
    }

    [Fact]
    public void TransformationResult_CanCreateUnmodifiedResult()
    {
        // Arrange & Act
        var result = new TransformationResult
        {
            FilePath = "/src/Config.cs",
            OriginalContent = "public class Config { }",
            NewContent = "public class Config { }",
            WasModified = false,
            LinesChanged = 0,
            IsHandler = false
        };

        // Assert
        Assert.Equal("/src/Config.cs", result.FilePath);
        Assert.False(result.WasModified);
        Assert.Equal(0, result.LinesChanged);
        Assert.False(result.IsHandler);
        Assert.Null(result.Error);
    }
}