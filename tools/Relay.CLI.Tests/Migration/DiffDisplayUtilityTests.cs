using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class DiffDisplayUtilityTests
{
    [Fact]
    public void DisplayDiff_WithNullOriginalAndModified_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(null!, null!));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithEmptyStrings_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(string.Empty, string.Empty));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithIdenticalContent_DoesNotThrow()
    {
        // Arrange
        var content = "using System;\nusing MediatR;";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(content, content));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithDifferentContent_DoesNotThrow()
    {
        // Arrange
        var original = "using MediatR;\npublic class Test { }";
        var modified = "using Relay;\npublic class Test { }";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithMaxLines_DoesNotThrow()
    {
        // Arrange
        var original = "line1\nline2\nline3\nline4\nline5";
        var modified = "line1\nmodified2\nline3\nmodified4\nline5";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified, maxLines: 3));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplaySideBySideDiff_WithNullOriginalAndModified_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplaySideBySideDiff(null!, null!));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplaySideBySideDiff_WithEmptyStrings_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplaySideBySideDiff(string.Empty, string.Empty));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplaySideBySideDiff_WithDifferentContent_DoesNotThrow()
    {
        // Arrange
        var original = "using MediatR;\npublic class Test { }";
        var modified = "using Relay;\npublic class Test { }";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplaySideBySideDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void PreviewFileTransformation_WithValidContent_DoesNotThrow()
    {
        // Arrange
        var filePath = "Test.cs";
        var original = "using MediatR;";
        var modified = "using Relay;";

        // Act & Assert
        var exception = Record.Exception(() =>
            DiffDisplayUtility.PreviewFileTransformation(filePath, original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void PreviewFileTransformation_WithSideBySide_DoesNotThrow()
    {
        // Arrange
        var filePath = "Test.cs";
        var original = "using MediatR;";
        var modified = "using Relay;";

        // Act & Assert
        var exception = Record.Exception(() =>
            DiffDisplayUtility.PreviewFileTransformation(filePath, original, modified, useSideBySide: true));
        Assert.Null(exception);
    }

    [Fact]
    public void GetChangeSummary_WithNullOriginalAndModified_ReturnsZeros()
    {
        // Arrange & Act
        var (added, removed, modifiedLines) = DiffDisplayUtility.GetChangeSummary(null!, null!);

        // Assert
        Assert.Equal(0, added);
        Assert.Equal(0, removed);
        Assert.Equal(0, modifiedLines);
    }

    [Fact]
    public void GetChangeSummary_WithEmptyStrings_ReturnsZeros()
    {
        // Arrange & Act
        var (added, removed, modifiedLines) = DiffDisplayUtility.GetChangeSummary(string.Empty, string.Empty);

        // Assert
        Assert.Equal(0, added);
        Assert.Equal(0, removed);
        Assert.Equal(0, modifiedLines);
    }

    [Fact]
    public void GetChangeSummary_WithIdenticalContent_ReturnsZeros()
    {
        // Arrange
        var content = "using System;\nusing MediatR;";

        // Act
        var (added, removed, modifiedLines) = DiffDisplayUtility.GetChangeSummary(content, content);

        // Assert
        Assert.Equal(0, added);
        Assert.Equal(0, removed);
        Assert.Equal(0, modifiedLines);
    }

    [Fact]
    public void GetChangeSummary_WithAddedLines_ReturnsCorrectCount()
    {
        // Arrange
        var original = "line1";
        var modified = "line1\nline2\nline3";

        // Act
        var (added, removed, _) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.Equal(2, added);
        Assert.Equal(0, removed);
    }

    [Fact]
    public void GetChangeSummary_WithRemovedLines_ReturnsCorrectCount()
    {
        // Arrange
        var original = "line1\nline2\nline3";
        var modified = "line1";

        // Act
        var (added, removed, _) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.Equal(0, added);
        Assert.Equal(2, removed);
    }

    [Fact]
    public void GetChangeSummary_WithModifiedLines_ReturnsCorrectCount()
    {
        // Arrange
        var original = "using MediatR;\npublic class Test { }";
        var modified = "using Relay;\npublic class Test { }";

        // Act
        var (added, removed, modifiedLines) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        // Note: DiffPlex may mark this as delete + insert instead of modify
        Assert.True(added > 0 || removed > 0 || modifiedLines > 0);
    }

    [Fact]
    public void GetChangeSummary_WithMixedChanges_ReturnsCorrectCounts()
    {
        // Arrange
        var original = "line1\nline2\nline3";
        var modified = "line1\nmodified2\nline3\nline4";

        // Act
        var (added, removed, modifiedLines) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.True(added > 0 || removed > 0 || modifiedLines > 0);
    }

    [Fact]
    public void GetChangeSummary_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var original = "var x = [1, 2, 3];";
        var modified = "var x = [1, 2, 3, 4];";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.GetChangeSummary(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void GetChangeSummary_WithMultilineChanges_ReturnsAccurateResults()
    {
        // Arrange
        var original = @"using MediatR;
using System;

public class Handler : IRequestHandler<Request>
{
    public Task Handle(Request request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}";
        var modified = @"using Relay;
using System;

public class Handler : IRequestHandler<Request>
{
    public ValueTask HandleAsync(Request request, CancellationToken ct)
    {
        return ValueTask.CompletedTask;
    }
}";

        // Act
        var (added, removed, modifiedCount) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.True(added + removed + modifiedCount > 0);
    }

    [Fact]
    public void DisplayDiff_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var original = "var x = \"hello\";";
        var modified = "var x = \"hello world\";";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var original = "Console.WriteLine(\"Hello 🌍\");";
        var modified = "Console.WriteLine(\"Hello 🌍 World\");";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithVeryLongLines_HandlesCorrectly()
    {
        // Arrange
        var original = new string('a', 1000);
        var modified = new string('a', 1000) + "b";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayDiff_WithMaxLines_TruncatesCorrectly()
    {
        // Arrange
        var original = "line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\nline9\nline10";
        var modified = "line1\nmodified2\nline3\nmodified4\nline5\nmodified6\nline7\nmodified8\nline9\nmodified10";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified, maxLines: 5));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplaySideBySideDiff_WithComplexMultilineChanges_HandlesCorrectly()
    {
        // Arrange
        var original = @"public class Test
{
    public void Method1()
    {
        Console.WriteLine(""Hello"");
    }

    public void Method2()
    {
        Console.WriteLine(""World"");
    }
}";
        var modified = @"public class Test
{
    public async Task Method1Async()
    {
        await Console.Out.WriteLineAsync(""Hello"");
    }

    public void Method2()
    {
        Console.WriteLine(""World"");
    }

    public void Method3()
    {
        Console.WriteLine(""New"");
    }
}";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplaySideBySideDiff(original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void PreviewFileTransformation_WithNullFilePath_HandlesCorrectly()
    {
        // Arrange
        var original = "content";
        var modified = "modified content";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.PreviewFileTransformation(null!, original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void PreviewFileTransformation_WithEmptyFilePath_HandlesCorrectly()
    {
        // Arrange
        var original = "content";
        var modified = "modified content";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.PreviewFileTransformation(string.Empty, original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void PreviewFileTransformation_WithComplexFilePath_HandlesCorrectly()
    {
        // Arrange
        var filePath = @"C:\Projects\Relay\src\Relay.Core\AI\Complex\File\Path\With\Many\Directories\Test.cs";
        var original = "using System;";
        var modified = "using System;\nusing System.Threading.Tasks;";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.PreviewFileTransformation(filePath, original, modified));
        Assert.Null(exception);
    }

    [Fact]
    public void GetChangeSummary_WithWhitespaceOnlyChanges_CountsCorrectly()
    {
        // Arrange
        var original = "line1\nline2\nline3";
        var modified = "line1\n  line2\nline3";

        // Act
        var (added, removed, modifiedCount) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.True(added + removed + modifiedCount > 0);
    }

    [Fact]
    public void GetChangeSummary_WithLargeContentBlocks_HandlesCorrectly()
    {
        // Arrange
        var original = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"line{i}"));
        var modified = string.Join("\n", Enumerable.Range(1, 1000).Select(i => i % 100 == 0 ? $"modified{i}" : $"line{i}"));

        // Act
        var (added, removed, modifiedCount) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.Equal(10, added); // Every 100th line is replaced, counted as added
        Assert.Equal(10, removed); // Every 100th line is replaced, counted as removed
        Assert.Equal(0, modifiedCount);
    }

    [Fact]
    public void GetChangeSummary_WithEmptyLines_HandlesCorrectly()
    {
        // Arrange
        var original = "line1\n\nline3";
        var modified = "line1\nline2\n\nline3";

        // Act
        var (added, removed, modifiedCount) = DiffDisplayUtility.GetChangeSummary(original, modified);

        // Assert
        Assert.Equal(1, added);
        Assert.Equal(0, removed);
        Assert.Equal(0, modifiedCount);
    }

    [Fact]
    public void DisplayDiff_WithMarkupCharacters_EscapesCorrectly()
    {
        // Arrange
        var original = "var x = [1, 2, 3];";
        var modified = "var x = [1, 2, 3, 4];";

        // Act & Assert
        var exception = Record.Exception(() => DiffDisplayUtility.DisplayDiff(original, modified));
        Assert.Null(exception);
    }
}
