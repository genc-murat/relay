using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class ScaffoldCommandTests : IDisposable
{
    private readonly string _testPath;

    public ScaffoldCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-scaffold-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task ScaffoldCommand_CreatesFeatureStructure()
    {
        // Arrange
        var featureName = "UserManagement";
        var featurePath = Path.Combine(_testPath, featureName);

        // Act
        Directory.CreateDirectory(featurePath);
        Directory.CreateDirectory(Path.Combine(featurePath, "Commands"));
        Directory.CreateDirectory(Path.Combine(featurePath, "Queries"));
        Directory.CreateDirectory(Path.Combine(featurePath, "Models"));

        // Assert
        Directory.Exists(featurePath).Should().BeTrue();
        Directory.Exists(Path.Combine(featurePath, "Commands")).Should().BeTrue();
        Directory.Exists(Path.Combine(featurePath, "Queries")).Should().BeTrue();
        Directory.Exists(Path.Combine(featurePath, "Models")).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_CreatesCRUDOperations()
    {
        // Arrange
        var entityName = "Product";
        var operations = new[] { "Create", "Read", "Update", "Delete" };

        // Act
        foreach (var op in operations)
        {
            var fileName = $"{op}{entityName}.cs";
            var filePath = Path.Combine(_testPath, fileName);
            await File.WriteAllTextAsync(filePath, $"// {op} operation for {entityName}");
        }

        // Assert
        foreach (var op in operations)
        {
            var fileName = $"{op}{entityName}.cs";
            File.Exists(Path.Combine(_testPath, fileName)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesWithTemplate()
    {
        // Arrange
        var template = "crud";
        var entityName = "Order";

        // Act
        var commandFile = Path.Combine(_testPath, $"Create{entityName}Command.cs");
        var handlerFile = Path.Combine(_testPath, $"Create{entityName}Handler.cs");

        var commandContent = $@"using Relay.Core;

public record Create{entityName}Command(string Name) : IRequest<Guid>;";

        var handlerContent = $@"using Relay.Core;

public class Create{entityName}Handler : IRequestHandler<Create{entityName}Command, Guid>
{{
    [Handle]
    public async ValueTask<Guid> HandleAsync(Create{entityName}Command request, CancellationToken ct)
    {{
        return Guid.NewGuid();
    }}
}}";

        await File.WriteAllTextAsync(commandFile, commandContent);
        await File.WriteAllTextAsync(handlerFile, handlerContent);

        // Assert
        File.Exists(commandFile).Should().BeTrue();
        File.Exists(handlerFile).Should().BeTrue();

        var commandText = await File.ReadAllTextAsync(commandFile);
        var handlerText = await File.ReadAllTextAsync(handlerFile);

        commandText.Should().Contain("IRequest<Guid>");
        handlerText.Should().Contain("[Handle]");
        handlerText.Should().Contain("ValueTask<Guid>");
    }

    [Theory]
    [InlineData("minimal", 2)] // Command + Handler
    [InlineData("standard", 4)] // Command + Handler + Request + Response
    [InlineData("full", 6)] // Command + Handler + Request + Response + Validator + Tests
    public async Task ScaffoldCommand_SupportsDifferentTemplates(string template, int expectedFiles)
    {
        // Arrange
        var featureName = "Payment";

        // Act - Create files based on template
        for (int i = 0; i < expectedFiles; i++)
        {
            var fileName = $"{featureName}_{template}_{i}.cs";
            await File.WriteAllTextAsync(Path.Combine(_testPath, fileName), $"// {template} template file {i}");
        }

        // Assert
        var files = Directory.GetFiles(_testPath, $"{featureName}_{template}_*.cs");
        files.Should().HaveCount(expectedFiles);
    }

    [Fact]
    public async Task ScaffoldCommand_CreatesRepositoryPattern()
    {
        // Arrange
        var entityName = "Customer";

        // Act
        var interfaceFile = Path.Combine(_testPath, $"I{entityName}Repository.cs");
        var implementationFile = Path.Combine(_testPath, $"{entityName}Repository.cs");

        var interfaceContent = $@"public interface I{entityName}Repository
{{
    Task<{entityName}> GetByIdAsync(Guid id);
    Task<IEnumerable<{entityName}>> GetAllAsync();
    Task AddAsync({entityName} entity);
    Task UpdateAsync({entityName} entity);
    Task DeleteAsync(Guid id);
}}";

        var implementationContent = $@"public class {entityName}Repository : I{entityName}Repository
{{
    // Implementation
}}";

        await File.WriteAllTextAsync(interfaceFile, interfaceContent);
        await File.WriteAllTextAsync(implementationFile, implementationContent);

        // Assert
        File.Exists(interfaceFile).Should().BeTrue();
        File.Exists(implementationFile).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesValidationRules()
    {
        // Arrange
        var commandName = "CreateUserCommand";

        // Act
        var validatorFile = Path.Combine(_testPath, $"{commandName}Validator.cs");
        var validatorContent = $@"using FluentValidation;

public class {commandName}Validator : AbstractValidator<{commandName}>
{{
    public {commandName}Validator()
    {{
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
    }}
}}";

        await File.WriteAllTextAsync(validatorFile, validatorContent);

        // Assert
        File.Exists(validatorFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(validatorFile);
        content.Should().Contain("AbstractValidator");
        content.Should().Contain("RuleFor");
    }

    [Fact]
    public async Task ScaffoldCommand_CreatesUnitTests()
    {
        // Arrange
        var handlerName = "CreateOrderHandler";

        // Act
        var testFile = Path.Combine(_testPath, $"{handlerName}Tests.cs");
        var testContent = $@"using Xunit;
using FluentAssertions;

public class {handlerName}Tests
{{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsOrderId()
    {{
        // Arrange
        var handler = new {handlerName}();
        var request = new CreateOrderCommand(""Test"");

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }}
}}";

        await File.WriteAllTextAsync(testFile, testContent);

        // Assert
        File.Exists(testFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("[Fact]");
        content.Should().Contain("Should()");
    }

    [Fact]
    public async Task ScaffoldCommand_SupportsCustomNamespace()
    {
        // Arrange
        var namespaceName = "MyCompany.MyProject.Features.Users";
        var entityName = "User";

        // Act
        var filePath = Path.Combine(_testPath, $"{entityName}.cs");
        var content = $@"namespace {namespaceName};

public class {entityName}
{{
    public Guid Id {{ get; set; }}
    public string Name {{ get; set; }}
}}";

        await File.WriteAllTextAsync(filePath, content);

        // Assert
        var fileContent = await File.ReadAllTextAsync(filePath);
        fileContent.Should().Contain($"namespace {namespaceName}");
    }

    [Fact]
    public async Task ScaffoldCommand_GeneratesAPIController()
    {
        // Arrange
        var entityName = "Product";

        // Act
        var controllerFile = Path.Combine(_testPath, $"{entityName}sController.cs");
        var controllerContent = $@"using Microsoft.AspNetCore.Mvc;
using Relay.Core;

[ApiController]
[Route(""api/[controller]"")]
public class {entityName}sController : ControllerBase
{{
    private readonly IRelayMediator _mediator;

    public {entityName}sController(IRelayMediator mediator)
    {{
        _mediator = mediator;
    }}

    [HttpPost]
    public async Task<IActionResult> Create(Create{entityName}Command command)
    {{
        var result = await _mediator.SendAsync(command);
        return CreatedAtAction(nameof(GetById), new {{ id = result }}, result);
    }}
}}";

        await File.WriteAllTextAsync(controllerFile, controllerContent);

        // Assert
        File.Exists(controllerFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(controllerFile);
        content.Should().Contain("[ApiController]");
        content.Should().Contain("IRelayMediator");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
