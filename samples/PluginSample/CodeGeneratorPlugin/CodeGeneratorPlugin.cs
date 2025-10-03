using Relay.CLI.Plugins;
using System.Text;

namespace CodeGeneratorPlugin;

/// <summary>
/// A plugin that generates boilerplate code for common patterns
/// </summary>
[RelayPlugin("code-generator", "1.0.0")]
public class CodeGeneratorPlugin : IRelayPlugin
{
    public string Name => "Code Generator";
    public string Version => "1.0.0";
    public string Description => "Generates boilerplate code for controllers, services, models, and more";
    public string[] Authors => new[] { "Relay Team" };
    public string[] Tags => new[] { "generator", "code", "scaffold", "boilerplate" };
    public string MinimumRelayVersion => "2.1.0";

    private IPluginContext? _context;

    public Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _context.Logger.LogInformation("Code Generator Plugin initialized");
        return Task.FromResult(true);
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_context == null)
        {
            Console.WriteLine("Plugin not initialized");
            return 1;
        }

        if (args.Length == 0)
        {
            Console.WriteLine(GetHelp());
            return 0;
        }

        try
        {
            var options = ParseArguments(args);

            if (string.IsNullOrEmpty(options.Type))
            {
                _context.Logger.LogError("--type is required");
                return 1;
            }

            if (string.IsNullOrEmpty(options.Name))
            {
                _context.Logger.LogError("--name is required");
                return 1;
            }

            var outputPath = options.Output ?? _context.WorkingDirectory;
            await GenerateCodeAsync(options, outputPath);

            _context.Logger.LogInformation($"✅ Successfully generated {options.Type}: {options.Name}");
            return 0;
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"Failed to generate code: {ex.Message}", ex);
            return 1;
        }
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _context?.Logger.LogInformation("Code Generator Plugin cleaned up");
        return Task.CompletedTask;
    }

    public string GetHelp()
    {
        return @"
Code Generator Plugin - Generate boilerplate code

Usage:
  relay plugin run code-generator --type <type> --name <name> [options]

Options:
  --type <type>        Type of code to generate (required)
                       Options: controller, service, repository, model, validator
  --name <name>        Name of the generated code (required)
  --output <path>      Output directory (default: current directory)
  --namespace <ns>     Custom namespace (default: auto-detected)
  --async              Generate async methods (default: true)
  --interface          Generate interface along with class (default: true)

Examples:
  # Generate a controller
  relay plugin run code-generator --type controller --name ProductController

  # Generate a service with interface
  relay plugin run code-generator --type service --name ProductService --interface

  # Generate a repository
  relay plugin run code-generator --type repository --name ProductRepository --output ./Infrastructure

  # Generate a model
  relay plugin run code-generator --type model --name Product --namespace MyApp.Models
";
    }

    private GeneratorOptions ParseArguments(string[] args)
    {
        var options = new GeneratorOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--type":
                    if (i + 1 < args.Length)
                        options.Type = args[++i];
                    break;
                case "--name":
                    if (i + 1 < args.Length)
                        options.Name = args[++i];
                    break;
                case "--output":
                    if (i + 1 < args.Length)
                        options.Output = args[++i];
                    break;
                case "--namespace":
                    if (i + 1 < args.Length)
                        options.Namespace = args[++i];
                    break;
                case "--async":
                    options.UseAsync = true;
                    break;
                case "--interface":
                    options.GenerateInterface = true;
                    break;
            }
        }

        return options;
    }

    private async Task GenerateCodeAsync(GeneratorOptions options, string outputPath)
    {
        if (_context == null) return;

        var namespaceName = options.Namespace ?? DetectNamespace(outputPath);
        
        switch (options.Type.ToLower())
        {
            case "controller":
                await GenerateControllerAsync(options.Name, namespaceName, outputPath);
                break;
            case "service":
                await GenerateServiceAsync(options.Name, namespaceName, outputPath, options.GenerateInterface);
                break;
            case "repository":
                await GenerateRepositoryAsync(options.Name, namespaceName, outputPath, options.GenerateInterface);
                break;
            case "model":
                await GenerateModelAsync(options.Name, namespaceName, outputPath);
                break;
            case "validator":
                await GenerateValidatorAsync(options.Name, namespaceName, outputPath);
                break;
            default:
                throw new ArgumentException($"Unknown type: {options.Type}");
        }
    }

    private async Task GenerateControllerAsync(string name, string namespaceName, string outputPath)
    {
        if (_context == null) return;

        var className = name.EndsWith("Controller") ? name : $"{name}Controller";
        var entityName = className.Replace("Controller", "");
        
        var content = $@"using Microsoft.AspNetCore.Mvc;
using {namespaceName}.Application.Services;
using {namespaceName}.Application.DTOs;

namespace {namespaceName}.Api.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class {className} : ControllerBase
{{
    private readonly I{entityName}Service _service;

    public {className}(I{entityName}Service service)
    {{
        _service = service;
    }}

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {{
        var result = await _service.GetAllAsync(cancellationToken);
        return Ok(result);
    }}

    [HttpGet(""{{id}}"")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {{
        var result = await _service.GetByIdAsync(id, cancellationToken);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }}

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Create{entityName}Request request, CancellationToken cancellationToken = default)
    {{
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new {{ id = result.Id }}, result);
    }}

    [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Update{entityName}Request request, CancellationToken cancellationToken = default)
    {{
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }}

    [HttpDelete(""{{id}}"")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {{
        var success = await _service.DeleteAsync(id, cancellationToken);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }}
}}
";

        var filePath = Path.Combine(outputPath, $"{className}.cs");
        await _context.FileSystem.WriteFileAsync(filePath, content);
        _context.Logger.LogInformation($"Generated controller: {filePath}");
    }

    private async Task GenerateServiceAsync(string name, string namespaceName, string outputPath, bool generateInterface)
    {
        if (_context == null) return;

        var className = name.EndsWith("Service") ? name : $"{name}Service";
        var interfaceName = $"I{className}";
        var entityName = className.Replace("Service", "");

        if (generateInterface)
        {
            var interfaceContent = $@"namespace {namespaceName}.Application.Services;

public interface {interfaceName}
{{
    Task<IEnumerable<{entityName}Dto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<{entityName}Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<{entityName}Dto> CreateAsync(Create{entityName}Request request, CancellationToken cancellationToken = default);
    Task<{entityName}Dto?> UpdateAsync(Guid id, Update{entityName}Request request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}}
";
            var interfacePath = Path.Combine(outputPath, $"{interfaceName}.cs");
            await _context.FileSystem.WriteFileAsync(interfacePath, interfaceContent);
            _context.Logger.LogInformation($"Generated interface: {interfacePath}");
        }

        var classContent = $@"using {namespaceName}.Domain.Repositories;
using {namespaceName}.Domain.Entities;

namespace {namespaceName}.Application.Services;

public class {className} : {interfaceName}
{{
    private readonly I{entityName}Repository _repository;

    public {className}(I{entityName}Repository repository)
    {{
        _repository = repository;
    }}

    public async Task<IEnumerable<{entityName}Dto>> GetAllAsync(CancellationToken cancellationToken = default)
    {{
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto);
    }}

    public async Task<{entityName}Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {{
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity != null ? MapToDto(entity) : null;
    }}

    public async Task<{entityName}Dto> CreateAsync(Create{entityName}Request request, CancellationToken cancellationToken = default)
    {{
        var entity = MapToEntity(request);
        var created = await _repository.AddAsync(entity, cancellationToken);
        return MapToDto(created);
    }}

    public async Task<{entityName}Dto?> UpdateAsync(Guid id, Update{entityName}Request request, CancellationToken cancellationToken = default)
    {{
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (entity == null)
            return null;

        // Update entity properties here
        
        var updated = await _repository.UpdateAsync(entity, cancellationToken);
        return MapToDto(updated);
    }}

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {{
        return await _repository.DeleteAsync(id, cancellationToken);
    }}

    private {entityName}Dto MapToDto({entityName} entity)
    {{
        return new {entityName}Dto
        {{
            Id = entity.Id,
            // Map other properties
        }};
    }}

    private {entityName} MapToEntity(Create{entityName}Request request)
    {{
        return new {entityName}
        {{
            // Map properties from request
        }};
    }}
}}
";

        var classPath = Path.Combine(outputPath, $"{className}.cs");
        await _context.FileSystem.WriteFileAsync(classPath, classContent);
        _context.Logger.LogInformation($"Generated service: {classPath}");
    }

    private async Task GenerateRepositoryAsync(string name, string namespaceName, string outputPath, bool generateInterface)
    {
        if (_context == null) return;

        var className = name.EndsWith("Repository") ? name : $"{name}Repository";
        var interfaceName = $"I{className}";
        var entityName = className.Replace("Repository", "");

        if (generateInterface)
        {
            var interfaceContent = $@"namespace {namespaceName}.Domain.Repositories;

public interface {interfaceName}
{{
    Task<IEnumerable<{entityName}>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<{entityName}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<{entityName}> AddAsync({entityName} entity, CancellationToken cancellationToken = default);
    Task<{entityName}> UpdateAsync({entityName} entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}}
";
            var interfacePath = Path.Combine(outputPath, $"{interfaceName}.cs");
            await _context.FileSystem.WriteFileAsync(interfacePath, interfaceContent);
            _context.Logger.LogInformation($"Generated repository interface: {interfacePath}");
        }

        var classContent = $@"using Microsoft.EntityFrameworkCore;
using {namespaceName}.Domain.Entities;
using {namespaceName}.Infrastructure.Data;

namespace {namespaceName}.Infrastructure.Repositories;

public class {className} : {interfaceName}
{{
    private readonly ApplicationDbContext _context;

    public {className}(ApplicationDbContext context)
    {{
        _context = context;
    }}

    public async Task<IEnumerable<{entityName}>> GetAllAsync(CancellationToken cancellationToken = default)
    {{
        return await _context.Set<{entityName}>()
            .ToListAsync(cancellationToken);
    }}

    public async Task<{entityName}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {{
        return await _context.Set<{entityName}>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }}

    public async Task<{entityName}> AddAsync({entityName} entity, CancellationToken cancellationToken = default)
    {{
        await _context.Set<{entityName}>().AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }}

    public async Task<{entityName}> UpdateAsync({entityName} entity, CancellationToken cancellationToken = default)
    {{
        _context.Set<{entityName}>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }}

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {{
        var entity = await GetByIdAsync(id, cancellationToken);
        
        if (entity == null)
            return false;

        _context.Set<{entityName}>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }}
}}
";

        var classPath = Path.Combine(outputPath, $"{className}.cs");
        await _context.FileSystem.WriteFileAsync(classPath, classContent);
        _context.Logger.LogInformation($"Generated repository: {classPath}");
    }

    private async Task GenerateModelAsync(string name, string namespaceName, string outputPath)
    {
        if (_context == null) return;

        var content = $@"namespace {namespaceName}.Domain.Entities;

public class {name}
{{
    public Guid Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }}
    public DateTime? UpdatedAt {{ get; set; }}
    
    // Add your properties here
}}

// DTOs
public record {name}Dto
{{
    public Guid Id {{ get; init; }}
    public DateTime CreatedAt {{ get; init; }}
    public DateTime? UpdatedAt {{ get; init; }}
}}

public record Create{name}Request
{{
    // Add your properties here
}}

public record Update{name}Request
{{
    // Add your properties here
}}
";

        var filePath = Path.Combine(outputPath, $"{name}.cs");
        await _context.FileSystem.WriteFileAsync(filePath, content);
        _context.Logger.LogInformation($"Generated model: {filePath}");
    }

    private async Task GenerateValidatorAsync(string name, string namespaceName, string outputPath)
    {
        if (_context == null) return;

        var className = name.EndsWith("Validator") ? name : $"{name}Validator";
        var modelName = className.Replace("Validator", "");

        var content = $@"using FluentValidation;

namespace {namespaceName}.Application.Validators;

public class {className} : AbstractValidator<{modelName}>
{{
    public {className}()
    {{
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(""Id is required"");

        // Add more validation rules here
    }}
}}
";

        var filePath = Path.Combine(outputPath, $"{className}.cs");
        await _context.FileSystem.WriteFileAsync(filePath, content);
        _context.Logger.LogInformation($"Generated validator: {filePath}");
    }

    private string DetectNamespace(string path)
    {
        // Simple namespace detection based on directory structure
        var dirName = Path.GetFileName(path);
        return string.IsNullOrEmpty(dirName) ? "MyApp" : dirName;
    }
}

internal class GeneratorOptions
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Output { get; set; }
    public string? Namespace { get; set; }
    public bool UseAsync { get; set; } = true;
    public bool GenerateInterface { get; set; } = true;
}
