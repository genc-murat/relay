using Relay.CLI.Migration;

namespace Relay.CLI.Tests.Migration;

public class CodeTransformerTests
{
    private readonly CodeTransformer _transformer;
    private readonly MigrationOptions _options;

    public CodeTransformerTests()
    {
        _transformer = new CodeTransformer();
        _options = new MigrationOptions
        {
            Aggressive = false,
            DryRun = false,
            ProjectPath = Path.GetTempPath()
        };
    }

    [Fact]
    public async Task TransformFileAsync_ChangesUsingDirective()
    {
        // Arrange
        var content = @"using MediatR;
using System;

public class TestClass { }";

        // Act
        var result = await _transformer.TransformFileAsync("test.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.Contains("Relay.Core", result.NewContent);
        Assert.DoesNotContain("MediatR", result.NewContent);
        Assert.Contains(result.Changes, c => c.Category == "Using Directives");
    }

    [Fact]
    public async Task TransformFileAsync_RenamesHandleToHandleAsync()
    {
        // Arrange
        var content = @"using MediatR;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("test.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.Contains("HandleAsync", result.NewContent);
        Assert.Contains(result.Changes, c => 
            c.Category == "Method Signatures" && 
            c.Type == ChangeType.Modify);
    }

    [Fact]
    public async Task TransformFileAsync_ConvertsTaskToValueTask()
    {
        // Arrange
        var content = @"using MediatR;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("test.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.Contains("ValueTask<string>", result.NewContent);
        Assert.Contains(result.Changes, c => 
            c.Category == "Return Types" && 
            c.Description.Contains("ValueTask"));
    }
}
