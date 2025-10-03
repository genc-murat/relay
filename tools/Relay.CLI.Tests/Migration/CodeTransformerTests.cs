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
        result.WasModified.Should().BeTrue();
        result.NewContent.Should().Contain("Relay.Core");
        result.NewContent.Should().NotContain("MediatR");
        result.Changes.Should().Contain(c => c.Category == "Using Directives");
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
        result.WasModified.Should().BeTrue();
        result.NewContent.Should().Contain("HandleAsync");
        result.Changes.Should().Contain(c => 
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
        result.WasModified.Should().BeTrue();
        result.NewContent.Should().Contain("ValueTask<string>");
        result.Changes.Should().Contain(c => 
            c.Category == "Return Types" && 
            c.Description.Contains("ValueTask"));
    }
}
