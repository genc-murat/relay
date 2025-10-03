namespace Relay.CLI.TemplateEngine;

/// <summary>
/// Validates template structure and configuration
/// </summary>
public class TemplateValidator
{
    public async Task<ValidationResult> ValidateAsync(string templatePath)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            // Check if template directory exists
            if (!Directory.Exists(templatePath))
            {
                result.IsValid = false;
                result.Errors.Add($"Template directory not found: {templatePath}");
                return result;
            }

            // Check for .template.config directory
            var configDir = Path.Combine(templatePath, ".template.config");
            if (!Directory.Exists(configDir))
            {
                result.IsValid = false;
                result.Errors.Add("Missing .template.config directory");
                return result;
            }

            // Check for template.json
            var templateJsonPath = Path.Combine(configDir, "template.json");
            if (!File.Exists(templateJsonPath))
            {
                result.IsValid = false;
                result.Errors.Add("Missing template.json file");
                return result;
            }

            // Validate template.json structure
            await ValidateTemplateJsonAsync(templateJsonPath, result);

            // Check for content directory
            var contentDir = Path.Combine(templatePath, "content");
            if (!Directory.Exists(contentDir))
            {
                result.Warnings.Add("No content directory found");
            }
            else
            {
                // Validate content structure
                ValidateContentStructure(contentDir, result);
            }

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                result.Message = "✅ Template validation passed";
            }
            else if (result.Errors.Count == 0)
            {
                result.Message = $"⚠️  Template validation passed with {result.Warnings.Count} warning(s)";
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }
        
        return result;
    }

    private async Task ValidateTemplateJsonAsync(string templateJsonPath, ValidationResult result)
    {
        try
        {
            var json = await File.ReadAllTextAsync(templateJsonPath);
            var template = System.Text.Json.JsonSerializer.Deserialize<TemplateMetadata>(json);

            if (template == null)
            {
                result.Errors.Add("Invalid template.json: Failed to deserialize");
                return;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(template.Name))
            {
                result.Errors.Add("template.json: 'name' is required");
            }

            if (string.IsNullOrEmpty(template.ShortName))
            {
                result.Errors.Add("template.json: 'shortName' is required");
            }

            if (string.IsNullOrEmpty(template.Identity))
            {
                result.Warnings.Add("template.json: 'identity' is not set");
            }

            if (template.Classifications == null || template.Classifications.Length == 0)
            {
                result.Warnings.Add("template.json: No classifications specified");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating template.json: {ex.Message}");
        }
    }

    private void ValidateContentStructure(string contentDir, ValidationResult result)
    {
        var files = Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories);
        
        if (files.Length == 0)
        {
            result.Warnings.Add("Content directory is empty");
        }

        // Check for common project files
        var hasProjectFile = files.Any(f => f.EndsWith(".csproj") || f.EndsWith(".sln"));
        if (!hasProjectFile)
        {
            result.Warnings.Add("No .csproj or .sln files found in content");
        }

        // Check for README
        var hasReadme = files.Any(f => Path.GetFileName(f).Equals("README.md", StringComparison.OrdinalIgnoreCase));
        if (!hasReadme)
        {
            result.Warnings.Add("No README.md found in content");
        }
    }

    public ValidationResult ValidateProjectName(string projectName)
    {
        var result = new ValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(projectName))
        {
            result.IsValid = false;
            result.Errors.Add("Project name cannot be empty");
            return result;
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (projectName.Any(c => invalidChars.Contains(c)))
        {
            result.IsValid = false;
            result.Errors.Add("Project name contains invalid characters");
        }

        // Check for C# naming conventions
        if (!char.IsLetter(projectName[0]))
        {
            result.Warnings.Add("Project name should start with a letter");
        }

        if (projectName.Contains(' '))
        {
            result.Warnings.Add("Project name contains spaces (PascalCase is recommended)");
        }

        // Check for reserved keywords
        var reservedKeywords = new[] { "System", "Microsoft", "Console", "Object" };
        if (reservedKeywords.Any(k => projectName.Equals(k, StringComparison.OrdinalIgnoreCase)))
        {
            result.Warnings.Add($"Project name '{projectName}' is a reserved keyword");
        }

        return result;
    }
}

public class TemplateMetadata
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string[] Classifications { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public void DisplayResults()
    {
        Console.WriteLine();
        
        if (IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Validation Failed");
            Console.ResetColor();
        }

        if (Errors.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Errors:");
            foreach (var error in Errors)
            {
                Console.WriteLine($"  • {error}");
            }
            Console.ResetColor();
        }

        if (Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warnings:");
            foreach (var warning in Warnings)
            {
                Console.WriteLine($"  • {warning}");
            }
            Console.ResetColor();
        }

        Console.WriteLine();
    }
}
