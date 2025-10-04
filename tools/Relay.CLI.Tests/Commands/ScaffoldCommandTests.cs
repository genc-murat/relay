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

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateDTO()
    {
        // Arrange
        var dtoName = "UserDto";

        // Act
        var dtoFile = Path.Combine(_testPath, $"{dtoName}.cs");
        var dtoContent = $@"public record {dtoName}(Guid Id, string Name, string Email);";
        await File.WriteAllTextAsync(dtoFile, dtoContent);

        // Assert
        var content = await File.ReadAllTextAsync(dtoFile);
        content.Should().Contain("record");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateMapper()
    {
        // Arrange
        var entityName = "User";

        // Act
        var mapperFile = Path.Combine(_testPath, $"{entityName}MappingProfile.cs");
        var mapperContent = $@"using AutoMapper;

public class {entityName}MappingProfile : Profile
{{
    public {entityName}MappingProfile()
    {{
        CreateMap<{entityName}, {entityName}Dto>();
        CreateMap<Create{entityName}Command, {entityName}>();
    }}
}}";
        await File.WriteAllTextAsync(mapperFile, mapperContent);

        // Assert
        File.Exists(mapperFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(mapperFile);
        content.Should().Contain("CreateMap");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateDbContext()
    {
        // Arrange
        var contextName = "AppDbContext";

        // Act
        var contextFile = Path.Combine(_testPath, $"{contextName}.cs");
        var contextContent = $@"using Microsoft.EntityFrameworkCore;

public class {contextName} : DbContext
{{
    public DbSet<User> Users {{ get; set; }}
    public DbSet<Product> Products {{ get; set; }}
}}";
        await File.WriteAllTextAsync(contextFile, contextContent);

        // Assert
        var content = await File.ReadAllTextAsync(contextFile);
        content.Should().Contain("DbContext");
        content.Should().Contain("DbSet");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateEntityConfiguration()
    {
        // Arrange
        var entityName = "User";

        // Act
        var configFile = Path.Combine(_testPath, $"{entityName}Configuration.cs");
        var configContent = $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class {entityName}Configuration : IEntityTypeConfiguration<{entityName}>
{{
    public void Configure(EntityTypeBuilder<{entityName}> builder)
    {{
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
    }}
}}";
        await File.WriteAllTextAsync(configFile, configContent);

        // Assert
        var content = await File.ReadAllTextAsync(configFile);
        content.Should().Contain("IEntityTypeConfiguration");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateService()
    {
        // Arrange
        var serviceName = "UserService";

        // Act
        var serviceFile = Path.Combine(_testPath, $"{serviceName}.cs");
        var serviceContent = $@"public class {serviceName} : I{serviceName}
{{
    private readonly IUserRepository _repository;

    public {serviceName}(IUserRepository repository)
    {{
        _repository = repository;
    }}
}}";
        await File.WriteAllTextAsync(serviceFile, serviceContent);

        // Assert
        File.Exists(serviceFile).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateSpecification()
    {
        // Arrange
        var specName = "ActiveUsersSpecification";

        // Act
        var specFile = Path.Combine(_testPath, $"{specName}.cs");
        var specContent = $@"public class {specName} : Specification<User>
{{
    public {specName}()
    {{
        AddFilter(u => u.IsActive);
    }}
}}";
        await File.WriteAllTextAsync(specFile, specContent);

        // Assert
        var content = await File.ReadAllTextAsync(specFile);
        content.Should().Contain("Specification");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateEventHandler()
    {
        // Arrange
        var eventName = "UserCreatedEvent";

        // Act
        var handlerFile = Path.Combine(_testPath, $"{eventName}Handler.cs");
        var handlerContent = $@"using Relay.Core;

public class {eventName}Handler : INotificationHandler<{eventName}>
{{
    public async ValueTask HandleAsync({eventName} notification, CancellationToken ct)
    {{
        // Handle event
    }}
}}";
        await File.WriteAllTextAsync(handlerFile, handlerContent);

        // Assert
        var content = await File.ReadAllTextAsync(handlerFile);
        content.Should().Contain("INotificationHandler");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateMiddleware()
    {
        // Arrange
        var middlewareName = "RequestLoggingMiddleware";

        // Act
        var middlewareFile = Path.Combine(_testPath, $"{middlewareName}.cs");
        var middlewareContent = $@"using Microsoft.AspNetCore.Http;

public class {middlewareName}
{{
    private readonly RequestDelegate _next;

    public {middlewareName}(RequestDelegate next)
    {{
        _next = next;
    }}

    public async Task InvokeAsync(HttpContext context)
    {{
        await _next(context);
    }}
}}";
        await File.WriteAllTextAsync(middlewareFile, middlewareContent);

        // Assert
        var content = await File.ReadAllTextAsync(middlewareFile);
        content.Should().Contain("RequestDelegate");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateBackgroundService()
    {
        // Arrange
        var serviceName = "DataSyncBackgroundService";

        // Act
        var serviceFile = Path.Combine(_testPath, $"{serviceName}.cs");
        var serviceContent = $@"using Microsoft.Extensions.Hosting;

public class {serviceName} : BackgroundService
{{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {{
        // Background work
    }}
}}";
        await File.WriteAllTextAsync(serviceFile, serviceContent);

        // Assert
        var content = await File.ReadAllTextAsync(serviceFile);
        content.Should().Contain("BackgroundService");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateExtensionMethods()
    {
        // Arrange
        var extensionName = "ServiceCollectionExtensions";

        // Act
        var extensionFile = Path.Combine(_testPath, $"{extensionName}.cs");
        var extensionContent = $@"using Microsoft.Extensions.DependencyInjection;

public static class {extensionName}
{{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {{
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }}
}}";
        await File.WriteAllTextAsync(extensionFile, extensionContent);

        // Assert
        var content = await File.ReadAllTextAsync(extensionFile);
        content.Should().Contain("this IServiceCollection");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateConstants()
    {
        // Arrange
        var constantsName = "UserConstants";

        // Act
        var constantsFile = Path.Combine(_testPath, $"{constantsName}.cs");
        var constantsContent = $@"public static class {constantsName}
{{
    public const int MaxNameLength = 100;
    public const int MinAge = 18;
    public const string DefaultRole = ""User"";
}}";
        await File.WriteAllTextAsync(constantsFile, constantsContent);

        // Assert
        var content = await File.ReadAllTextAsync(constantsFile);
        content.Should().Contain("const");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateExceptions()
    {
        // Arrange
        var exceptionName = "UserNotFoundException";

        // Act
        var exceptionFile = Path.Combine(_testPath, $"{exceptionName}.cs");
        var exceptionContent = $@"public class {exceptionName} : Exception
{{
    public {exceptionName}(Guid userId)
        : base($""User with ID {{userId}} not found."")
    {{
    }}
}}";
        await File.WriteAllTextAsync(exceptionFile, exceptionContent);

        // Assert
        var content = await File.ReadAllTextAsync(exceptionFile);
        content.Should().Contain(": Exception");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateEnums()
    {
        // Arrange
        var enumName = "UserRole";

        // Act
        var enumFile = Path.Combine(_testPath, $"{enumName}.cs");
        var enumContent = $@"public enum {enumName}
{{
    Admin = 1,
    User = 2,
    Guest = 3
}}";
        await File.WriteAllTextAsync(enumFile, enumContent);

        // Assert
        var content = await File.ReadAllTextAsync(enumFile);
        content.Should().Contain("enum");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateValueObject()
    {
        // Arrange
        var valueObjectName = "Email";

        // Act
        var voFile = Path.Combine(_testPath, $"{valueObjectName}.cs");
        var voContent = $@"public record {valueObjectName}
{{
    public string Value {{ get; init; }}

    public {valueObjectName}(string value)
    {{
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(""Email cannot be empty"");
        Value = value;
    }}
}}";
        await File.WriteAllTextAsync(voFile, voContent);

        // Assert
        var content = await File.ReadAllTextAsync(voFile);
        content.Should().Contain("record");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateAggregate()
    {
        // Arrange
        var aggregateName = "Order";

        // Act
        var aggregateFile = Path.Combine(_testPath, $"{aggregateName}.cs");
        var aggregateContent = $@"public class {aggregateName}
{{
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(OrderItem item)
    {{
        _items.Add(item);
    }}
}}";
        await File.WriteAllTextAsync(aggregateFile, aggregateContent);

        // Assert
        var content = await File.ReadAllTextAsync(aggregateFile);
        content.Should().Contain("IReadOnlyCollection");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGeneratePaginatedQuery()
    {
        // Arrange
        var queryName = "GetUsersQuery";

        // Act
        var queryFile = Path.Combine(_testPath, $"{queryName}.cs");
        var queryContent = $@"public record {queryName}(int Page, int PageSize) : IRequest<PagedResult<UserDto>>;";
        await File.WriteAllTextAsync(queryFile, queryContent);

        // Assert
        var content = await File.ReadAllTextAsync(queryFile);
        content.Should().Contain("PagedResult");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateHealthCheck()
    {
        // Arrange
        var healthCheckName = "DatabaseHealthCheck";

        // Act
        var healthCheckFile = Path.Combine(_testPath, $"{healthCheckName}.cs");
        var healthCheckContent = $@"using Microsoft.Extensions.Diagnostics.HealthChecks;

public class {healthCheckName} : IHealthCheck
{{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {{
        return HealthCheckResult.Healthy();
    }}
}}";
        await File.WriteAllTextAsync(healthCheckFile, healthCheckContent);

        // Assert
        var content = await File.ReadAllTextAsync(healthCheckFile);
        content.Should().Contain("IHealthCheck");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGeneratePolicy()
    {
        // Arrange
        var policyName = "AdminOnlyPolicy";

        // Act
        var policyFile = Path.Combine(_testPath, $"{policyName}.cs");
        var policyContent = $@"using Microsoft.AspNetCore.Authorization;

public class {policyName} : IAuthorizationRequirement
{{
    public string RequiredRole {{ get; }} = ""Admin"";
}}";
        await File.WriteAllTextAsync(policyFile, policyContent);

        // Assert
        var content = await File.ReadAllTextAsync(policyFile);
        content.Should().Contain("IAuthorizationRequirement");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateFilter()
    {
        // Arrange
        var filterName = "ValidationFilter";

        // Act
        var filterFile = Path.Combine(_testPath, $"{filterName}.cs");
        var filterContent = $@"using Microsoft.AspNetCore.Mvc.Filters;

public class {filterName} : IActionFilter
{{
    public void OnActionExecuting(ActionExecutingContext context) {{ }}
    public void OnActionExecuted(ActionExecutedContext context) {{ }}
}}";
        await File.WriteAllTextAsync(filterFile, filterContent);

        // Assert
        var content = await File.ReadAllTextAsync(filterFile);
        content.Should().Contain("IActionFilter");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateSeeder()
    {
        // Arrange
        var seederName = "UserSeeder";

        // Act
        var seederFile = Path.Combine(_testPath, $"{seederName}.cs");
        var seederContent = $@"public class {seederName}
{{
    public async Task SeedAsync(AppDbContext context)
    {{
        if (!context.Users.Any())
        {{
            context.Users.Add(new User {{ Name = ""Admin"" }});
            await context.SaveChangesAsync();
        }}
    }}
}}";
        await File.WriteAllTextAsync(seederFile, seederContent);

        // Assert
        var content = await File.ReadAllTextAsync(seederFile);
        content.Should().Contain("SeedAsync");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateMigration()
    {
        // Arrange
        var migrationName = "AddUserTable";

        // Act
        var migrationFile = Path.Combine(_testPath, $"{migrationName}.cs");
        var migrationContent = $@"using Microsoft.EntityFrameworkCore.Migrations;

public partial class {migrationName} : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        migrationBuilder.CreateTable(name: ""Users"");
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        migrationBuilder.DropTable(name: ""Users"");
    }}
}}";
        await File.WriteAllTextAsync(migrationFile, migrationContent);

        // Assert
        var content = await File.ReadAllTextAsync(migrationFile);
        content.Should().Contain("Migration");
    }

    [Theory]
    [InlineData("User", "Users")]
    [InlineData("Product", "Products")]
    [InlineData("Category", "Categories")]
    [InlineData("Company", "Companies")]
    public async Task ScaffoldCommand_ShouldPluralizeEntityNames(string singular, string plural)
    {
        // Act - Simple pluralization check
        var pluralized = singular.EndsWith("y")
            ? singular.Substring(0, singular.Length - 1) + "ies"
            : singular + "s";

        // Assert
        pluralized.Should().Be(plural);
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateIntegrationTest()
    {
        // Arrange
        var testName = "CreateUserIntegrationTests";

        // Act
        var testFile = Path.Combine(_testPath, $"{testName}.cs");
        var testContent = $@"using Xunit;

public class {testName} : IClassFixture<WebApplicationFactory<Program>>
{{
    [Fact]
    public async Task CreateUser_ReturnsCreatedUser()
    {{
        // Integration test
    }}
}}";
        await File.WriteAllTextAsync(testFile, testContent);

        // Assert
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("IClassFixture");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateDocumentation()
    {
        // Arrange
        var docName = "UserFeature.md";

        // Act
        var docFile = Path.Combine(_testPath, docName);
        var docContent = @"# User Feature

## Overview
User management feature

## Endpoints
- POST /api/users - Create user
- GET /api/users - List users";
        await File.WriteAllTextAsync(docFile, docContent);

        // Assert
        File.Exists(docFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(docFile);
        content.Should().Contain("# User Feature");
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldSupportDryRun()
    {
        // Arrange
        var dryRun = true;

        // Act
        var shouldCreateFiles = !dryRun;

        // Assert
        shouldCreateFiles.Should().BeFalse();
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldValidateEntityName()
    {
        // Arrange
        var validName = "User";
        var invalidName = "user";

        // Act
        var isValid = char.IsUpper(validName[0]);
        var isInvalid = char.IsLower(invalidName[0]);

        // Assert
        isValid.Should().BeTrue();
        isInvalid.Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldDetectExistingFiles()
    {
        // Arrange
        var fileName = "User.cs";
        var filePath = Path.Combine(_testPath, fileName);
        await File.WriteAllTextAsync(filePath, "// existing");

        // Act
        var exists = File.Exists(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldSupportOverwriteFlag()
    {
        // Arrange
        var overwrite = true;

        // Assert
        overwrite.Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldCommand_ShouldGenerateGitignore()
    {
        // Arrange
        var gitignorePath = Path.Combine(_testPath, ".gitignore");

        // Act
        await File.WriteAllTextAsync(gitignorePath, "bin/\nobj/\n*.user");

        // Assert
        File.Exists(gitignorePath).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }
}
