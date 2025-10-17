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

    [Fact]
    public async Task PreviewTransformAsync_ReadsFileAndTransforms()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = @"using MediatR;
using System;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var result = await _transformer.PreviewTransformAsync(tempFile);

            // Assert
            Assert.Equal(tempFile, result.FilePath);
            Assert.Equal(content, result.OriginalContent);
            Assert.True(result.WasModified);
            Assert.Contains("Relay.Core", result.NewContent);
            Assert.Contains("HandleAsync", result.NewContent);
            Assert.Contains("ValueTask<string>", result.NewContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task TransformFileAsync_TransformsDIRegistrations()
    {
        // Arrange
        var content = @"using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMediatR();
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("Startup.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.Contains("services.AddRelay()", result.NewContent);
        Assert.DoesNotContain("services.AddMediatR", result.NewContent);
        Assert.Contains(result.Changes, c =>
            c.Category == "DI Registration" &&
            c.Type == ChangeType.Modify);
    }

    [Fact]
    public async Task TransformFileAsync_AddsHandleAttributeInAggressiveMode()
    {
        // Arrange
        var aggressiveOptions = new MigrationOptions
        {
            Aggressive = true,
            DryRun = false,
            ProjectPath = Path.GetTempPath()
        };

        var content = @"using MediatR;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("test.cs", content, aggressiveOptions);

        // Assert
        Assert.True(result.WasModified);
        Assert.Contains("[Handle]", result.NewContent);
        Assert.Contains("HandleAsync", result.NewContent);
        Assert.Contains(result.Changes, c =>
            c.Category == "Attributes" &&
            c.Type == ChangeType.Add &&
            c.Description.Contains("[Handle]"));
    }

    [Fact]
    public async Task TransformFileAsync_TransformsNotificationHandler()
    {
        // Arrange
        var content = @"using MediatR;

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        // Handle notification
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("test.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.True(result.IsHandler);
        Assert.Contains("HandleAsync", result.NewContent);
        Assert.Contains(result.Changes, c =>
            c.Category == "Method Signatures" &&
            c.Type == ChangeType.Modify);
    }

    [Fact]
    public async Task TransformFileAsync_HandlesMalformedCodeGracefully()
    {
        // Arrange
        var malformedContent = @"using MediatR;

public class MalformedClass {
    // Missing closing brace and invalid syntax
    public void Method(";

        // Act
        var result = await _transformer.TransformFileAsync("malformed.cs", malformedContent, _options);

        // Assert
        // Roslyn can still parse this code, so transformation proceeds
        Assert.True(result.WasModified);
        Assert.Null(result.Error);
        Assert.Contains("Relay.Core", result.NewContent);
    }

    [Fact]
    public async Task TransformFileAsync_ReturnsUnchangedForNonMediatRCode()
    {
        // Arrange
        var content = @"using System;

public class RegularClass
{
    public void RegularMethod()
    {
        Console.WriteLine(""Hello World"");
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("regular.cs", content, _options);

        // Assert
        Assert.False(result.WasModified);
        Assert.False(result.IsHandler);
        Assert.Equal(content, result.NewContent);
        Assert.Empty(result.Changes);
        Assert.Equal(0, result.LinesChanged);
    }

    [Fact]
    public async Task TransformFileAsync_AppliesMultipleTransformationsInSingleFile()
    {
        // Arrange
        var content = @"using MediatR;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMediatR();
    }
}

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("complex.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.True(result.IsHandler);

        // Check using directive transformation
        Assert.Contains("Relay.Core", result.NewContent);
        Assert.DoesNotContain("using MediatR;", result.NewContent);

        // Check DI registration transformation
        Assert.Contains("services.AddRelay()", result.NewContent);
        Assert.DoesNotContain("services.AddMediatR", result.NewContent);

        // Check handler transformation
        Assert.Contains("HandleAsync", result.NewContent);
        Assert.Contains("ValueTask<string>", result.NewContent);

        // Check that multiple changes are recorded
        Assert.True(result.Changes.Count >= 3); // At least using, DI, and method changes
        Assert.Contains(result.Changes, c => c.Category == "Using Directives");
        Assert.Contains(result.Changes, c => c.Category == "DI Registration");
        Assert.Contains(result.Changes, c => c.Category == "Method Signatures");
        Assert.Contains(result.Changes, c => c.Category == "Return Types");
    }

    [Fact]
    public async Task TransformFileAsync_HandlesDifferentReturnTypes()
    {
        // Arrange
        var content = @"using MediatR;

public class VoidHandler : IRequestHandler<VoidRequest>
{
    public async Task Handle(VoidRequest request, CancellationToken cancellationToken)
    {
        // No return value
    }
}

public class StringHandler : IRequestHandler<StringRequest, string>
{
    public async Task<string> Handle(StringRequest request, CancellationToken cancellationToken)
    {
        return ""result"";
    }
}

public class IntHandler : IRequestHandler<IntRequest, int>
{
    public async Task<int> Handle(IntRequest request, CancellationToken cancellationToken)
    {
        return 42;
    }
}";

        // Act
        var result = await _transformer.TransformFileAsync("different-returns.cs", content, _options);

        // Assert
        Assert.True(result.WasModified);
        Assert.True(result.IsHandler);

        // Check that Handle methods are renamed to HandleAsync
        Assert.Contains("HandleAsync", result.NewContent);

        // Check that using directive was transformed
        Assert.Contains("Relay.Core", result.NewContent);

        // Check that the file was modified
        Assert.True(result.WasModified);
    }
}
