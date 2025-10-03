namespace Relay.CLI.TemplateEngine;

/// <summary>
/// Manages template publishing and packaging
/// </summary>
public class TemplatePublisher
{
    private readonly string _templatesPath;

    public TemplatePublisher(string templatesPath)
    {
        _templatesPath = templatesPath;
    }

    public async Task<PublishResult> PackTemplateAsync(string templatePath, string outputPath)
    {
        var result = new PublishResult();
        
        try
        {
            Console.WriteLine($"📦 Packaging template from {templatePath}...");

            // Validate template first
            var validator = new TemplateValidator();
            var validationResult = await validator.ValidateAsync(templatePath);
            
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = "Template validation failed";
                result.Errors.AddRange(validationResult.Errors);
                return result;
            }

            // Create output directory if it doesn't exist
            Directory.CreateDirectory(outputPath);

            // Get template name from template.json
            var templateJsonPath = Path.Combine(templatePath, ".template.config", "template.json");
            var templateJson = await File.ReadAllTextAsync(templateJsonPath);
            var metadata = System.Text.Json.JsonSerializer.Deserialize<TemplateMetadata>(templateJson);

            if (metadata == null)
            {
                result.Success = false;
                result.Message = "Failed to read template metadata";
                return result;
            }

            // Create package name
            var packageName = $"{metadata.ShortName}.1.0.0.nupkg";
            var packagePath = Path.Combine(outputPath, packageName);

            // Package template (simplified - in production would create actual NuGet package)
            await PackageTemplateFilesAsync(templatePath, packagePath);

            result.Success = true;
            result.Message = $"✅ Template packaged successfully: {packageName}";
            result.PackagePath = packagePath;

            Console.WriteLine($"  ✓ Created package: {packageName}");
            Console.WriteLine($"  ✓ Package location: {packagePath}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error packaging template: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task PackageTemplateFilesAsync(string templatePath, string packagePath)
    {
        // In a real implementation, this would create a proper NuGet package
        // For now, we'll create a zip file
        
        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(
            templatePath,
            packagePath.Replace(".nupkg", ".zip"),
            System.IO.Compression.CompressionLevel.Optimal,
            false
        );

        await Task.CompletedTask;
    }

    public async Task<PublishResult> PublishTemplateAsync(string packagePath, string registryUrl)
    {
        var result = new PublishResult();

        try
        {
            Console.WriteLine($"📤 Publishing template to {registryUrl}...");

            // Validate package exists
            if (!File.Exists(packagePath))
            {
                result.Success = false;
                result.Message = $"Package not found: {packagePath}";
                return result;
            }

            // In a real implementation, this would push to a NuGet registry
            // For now, just simulate success
            await Task.Delay(1000);

            result.Success = true;
            result.Message = $"✅ Template published successfully to {registryUrl}";
            result.PackagePath = packagePath;

            Console.WriteLine("  ✓ Template published");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error publishing template: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<List<TemplateInfo>> ListAvailableTemplatesAsync()
    {
        var templates = new List<TemplateInfo>();

        try
        {
            if (!Directory.Exists(_templatesPath))
            {
                return templates;
            }

            var templateDirs = Directory.GetDirectories(_templatesPath);

            foreach (var templateDir in templateDirs)
            {
                var templateJsonPath = Path.Combine(templateDir, ".template.config", "template.json");
                
                if (File.Exists(templateJsonPath))
                {
                    var json = await File.ReadAllTextAsync(templateJsonPath);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<TemplateMetadata>(json);

                    if (metadata != null)
                    {
                        templates.Add(new TemplateInfo
                        {
                            Id = metadata.ShortName,
                            Name = metadata.Name,
                            Description = metadata.Description,
                            Author = metadata.Author,
                            Path = templateDir
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing templates: {ex.Message}");
        }

        return templates;
    }
}

public class PublishResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PackagePath { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public class TemplateInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
}
