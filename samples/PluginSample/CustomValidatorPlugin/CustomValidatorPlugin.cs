using Relay.CLI.Plugins;
using System.Text.RegularExpressions;

namespace CustomValidatorPlugin;

/// <summary>
/// A plugin that provides custom validation rules for code quality
/// </summary>
[RelayPlugin("custom-validator", "1.0.0")]
public class CustomValidatorPlugin : IRelayPlugin
{
    public string Name => "Custom Validator";
    public string Version => "1.0.0";
    public string Description => "Provides custom validation rules for code quality and conventions";
    public string[] Authors => new[] { "Relay Team" };
    public string[] Tags => new[] { "validator", "quality", "linting", "conventions" };
    public string MinimumRelayVersion => "2.1.0";

    private IPluginContext? _context;

    public Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _context.Logger.LogInformation("Custom Validator Plugin initialized");
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

            if (string.IsNullOrEmpty(options.Rule) && !options.All)
            {
                _context.Logger.LogError("--rule is required (or use --all)");
                return 1;
            }

            var path = options.Path ?? _context.WorkingDirectory;

            if (!await _context.FileSystem.DirectoryExistsAsync(path))
            {
                _context.Logger.LogError($"Path not found: {path}");
                return 1;
            }

            var violations = await ValidateAsync(options, path);

            ReportResults(violations, options);

            return violations.Any(v => !options.WarningsOnly && v.Severity == Severity.Error) ? 1 : 0;
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"Validation failed: {ex.Message}", ex);
            return 1;
        }
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _context?.Logger.LogInformation("Custom Validator Plugin cleaned up");
        return Task.CompletedTask;
    }

    public string GetHelp()
    {
        return @"
Custom Validator Plugin - Validate code quality and conventions

Usage:
  relay plugin run custom-validator --rule <rule> [options]

Options:
  --rule <rule>        Validation rule to apply (required)
                       Options: naming, async-suffix, file-organization, 
                                documentation, error-handling, security
  --all                Run all validation rules
  --path <path>        Path to validate (default: current directory)
  --warnings-only      Treat all violations as warnings
  --fix                Attempt to auto-fix violations (where possible)
  --config <file>      Custom configuration file

Available Rules:
  naming               Validate naming conventions (PascalCase, camelCase, etc.)
  async-suffix         Ensure async methods have 'Async' suffix
  file-organization    Check file organization and structure
  documentation        Verify XML documentation comments
  error-handling       Check error handling patterns
  security             Security best practices (SQL injection, XSS, etc.)

Examples:
  # Check naming conventions
  relay plugin run custom-validator --rule naming --path ./src

  # Run all validations
  relay plugin run custom-validator --all --path ./src

  # Check async naming with auto-fix
  relay plugin run custom-validator --rule async-suffix --fix

  # Use custom config
  relay plugin run custom-validator --config .validator.json
";
    }

    private ValidatorOptions ParseArguments(string[] args)
    {
        var options = new ValidatorOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--rule":
                    if (i + 1 < args.Length)
                        options.Rule = args[++i];
                    break;
                case "--all":
                    options.All = true;
                    break;
                case "--path":
                    if (i + 1 < args.Length)
                        options.Path = args[++i];
                    break;
                case "--warnings-only":
                    options.WarningsOnly = true;
                    break;
                case "--fix":
                    options.Fix = true;
                    break;
                case "--config":
                    if (i + 1 < args.Length)
                        options.ConfigFile = args[++i];
                    break;
            }
        }

        return options;
    }

    private async Task<List<Violation>> ValidateAsync(ValidatorOptions options, string path)
    {
        var violations = new List<Violation>();

        if (options.All)
        {
            violations.AddRange(await ValidateNamingAsync(path));
            violations.AddRange(await ValidateAsyncSuffixAsync(path));
            violations.AddRange(await ValidateFileOrganizationAsync(path));
            violations.AddRange(await ValidateDocumentationAsync(path));
            violations.AddRange(await ValidateErrorHandlingAsync(path));
            violations.AddRange(await ValidateSecurityAsync(path));
        }
        else
        {
            violations.AddRange(options.Rule?.ToLower() switch
            {
                "naming" => await ValidateNamingAsync(path),
                "async-suffix" => await ValidateAsyncSuffixAsync(path),
                "file-organization" => await ValidateFileOrganizationAsync(path),
                "documentation" => await ValidateDocumentationAsync(path),
                "error-handling" => await ValidateErrorHandlingAsync(path),
                "security" => await ValidateSecurityAsync(path),
                _ => new List<Violation>()
            });
        }

        if (options.Fix)
        {
            await ApplyFixesAsync(violations);
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateNamingAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check class names (should be PascalCase)
                if (Regex.IsMatch(line, @"^\s*(public|internal|private)\s+class\s+([a-z])"))
                {
                    violations.Add(new Violation
                    {
                        Rule = "naming",
                        File = file,
                        Line = i + 1,
                        Message = "Class names should start with uppercase letter (PascalCase)",
                        Severity = Severity.Error,
                        Code = "NAMING001"
                    });
                }

                // Check private fields (should be _camelCase)
                if (Regex.IsMatch(line, @"^\s*private\s+\w+\s+([A-Z]\w+)\s*[;=]"))
                {
                    violations.Add(new Violation
                    {
                        Rule = "naming",
                        File = file,
                        Line = i + 1,
                        Message = "Private fields should start with underscore and lowercase letter (_camelCase)",
                        Severity = Severity.Warning,
                        Code = "NAMING002"
                    });
                }
            }
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateAsyncSuffixAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check async methods without Async suffix
                if (Regex.IsMatch(line, @"^\s*(public|private|protected|internal)\s+async\s+Task") &&
                    !Regex.IsMatch(line, @"\s+\w+Async\s*\("))
                {
                    violations.Add(new Violation
                    {
                        Rule = "async-suffix",
                        File = file,
                        Line = i + 1,
                        Message = "Async methods should have 'Async' suffix",
                        Severity = Severity.Warning,
                        Code = "ASYNC001",
                        Fixable = true
                    });
                }
            }
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateFileOrganizationAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);

            // Check if using statements are at the top
            var lines = content.Split('\n');
            var firstCodeLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));
            
            if (firstCodeLine != null && !firstCodeLine.TrimStart().StartsWith("using") && !firstCodeLine.TrimStart().StartsWith("namespace"))
            {
                violations.Add(new Violation
                {
                    Rule = "file-organization",
                    File = file,
                    Line = 1,
                    Message = "Using statements should be at the top of the file",
                    Severity = Severity.Info,
                    Code = "ORG001"
                });
            }
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateDocumentationAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check public methods without XML documentation
                if (Regex.IsMatch(line, @"^\s*public\s+(\w+)\s+\w+\s*\("))
                {
                    var hasDocs = i > 0 && lines[i - 1].TrimStart().StartsWith("///");
                    
                    if (!hasDocs)
                    {
                        violations.Add(new Violation
                        {
                            Rule = "documentation",
                            File = file,
                            Line = i + 1,
                            Message = "Public methods should have XML documentation comments",
                            Severity = Severity.Info,
                            Code = "DOC001"
                        });
                    }
                }
            }
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateErrorHandlingAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for empty catch blocks
                if (line.TrimStart().StartsWith("catch") && 
                    i + 1 < lines.Length && 
                    lines[i + 1].TrimStart().StartsWith("}"))
                {
                    violations.Add(new Violation
                    {
                        Rule = "error-handling",
                        File = file,
                        Line = i + 1,
                        Message = "Empty catch blocks should be avoided",
                        Severity = Severity.Warning,
                        Code = "ERR001"
                    });
                }

                // Check for catching generic Exception
                if (Regex.IsMatch(line, @"catch\s*\(\s*Exception\s+"))
                {
                    violations.Add(new Violation
                    {
                        Rule = "error-handling",
                        File = file,
                        Line = i + 1,
                        Message = "Consider catching specific exceptions instead of generic Exception",
                        Severity = Severity.Info,
                        Code = "ERR002"
                    });
                }
            }
        }

        return violations;
    }

    private async Task<List<Violation>> ValidateSecurityAsync(string path)
    {
        if (_context == null) return new List<Violation>();

        var violations = new List<Violation>();
        var files = await _context.FileSystem.GetFilesAsync(path, "*.cs", true);

        foreach (var file in files)
        {
            var content = await _context.FileSystem.ReadFileAsync(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for string concatenation in SQL queries
                if (Regex.IsMatch(line, @"(SELECT|INSERT|UPDATE|DELETE).+\+", RegexOptions.IgnoreCase))
                {
                    violations.Add(new Violation
                    {
                        Rule = "security",
                        File = file,
                        Line = i + 1,
                        Message = "Potential SQL injection vulnerability - use parameterized queries",
                        Severity = Severity.Error,
                        Code = "SEC001"
                    });
                }

                // Check for hardcoded passwords
                if (Regex.IsMatch(line, @"password\s*=\s*""[^""]+""", RegexOptions.IgnoreCase))
                {
                    violations.Add(new Violation
                    {
                        Rule = "security",
                        File = file,
                        Line = i + 1,
                        Message = "Hardcoded password detected - use secure configuration",
                        Severity = Severity.Error,
                        Code = "SEC002"
                    });
                }
            }
        }

        return violations;
    }

    private async Task ApplyFixesAsync(List<Violation> violations)
    {
        if (_context == null) return;

        var fixableViolations = violations.Where(v => v.Fixable).ToList();
        var fileGroups = fixableViolations.GroupBy(v => v.File);

        foreach (var fileGroup in fileGroups)
        {
            var filePath = fileGroup.Key;
            var content = await _context.FileSystem.ReadFileAsync(filePath);
            var lines = content.Split('\n').ToList();
            var modified = false;

            // Process violations in reverse order to maintain line numbers
            foreach (var violation in fileGroup.OrderByDescending(v => v.Line))
            {
                var lineIndex = violation.Line - 1;
                if (lineIndex < 0 || lineIndex >= lines.Count) continue;

                var line = lines[lineIndex];
                var fixedLine = ApplyFix(line, violation);

                if (fixedLine != line)
                {
                    lines[lineIndex] = fixedLine;
                    modified = true;
                    _context.Logger.LogInformation($"Fixed {violation.Code} in {Path.GetFileName(filePath)}:{violation.Line}");
                }
            }

            if (modified)
            {
                var fixedContent = string.Join('\n', lines);
                await _context.FileSystem.WriteFileAsync(filePath, fixedContent);
            }
        }

        if (fixableViolations.Any())
        {
            _context.Logger.LogInformation($"✅ Fixed {fixableViolations.Count} violation(s) in {fileGroups.Count()} file(s)");
        }
    }

    private string ApplyFix(string line, Violation violation)
    {
        switch (violation.Code)
        {
            case "ASYNC001": // Add Async suffix to async methods
                var match = Regex.Match(line, @"(\s+)(\w+)(\s*\()");
                if (match.Success)
                {
                    var methodName = match.Groups[2].Value;
                    if (!methodName.EndsWith("Async"))
                    {
                        return line.Replace(
                            $"{match.Groups[1].Value}{methodName}{match.Groups[3].Value}",
                            $"{match.Groups[1].Value}{methodName}Async{match.Groups[3].Value}"
                        );
                    }
                }
                break;

            case "NAMING002": // Fix private field naming (_camelCase)
                var fieldMatch = Regex.Match(line, @"(private\s+\w+\s+)([A-Z]\w+)(\s*[;=])");
                if (fieldMatch.Success)
                {
                    var fieldName = fieldMatch.Groups[2].Value;
                    var fixedName = "_" + char.ToLower(fieldName[0]) + fieldName.Substring(1);
                    return line.Replace(
                        $"{fieldMatch.Groups[1].Value}{fieldName}{fieldMatch.Groups[3].Value}",
                        $"{fieldMatch.Groups[1].Value}{fixedName}{fieldMatch.Groups[3].Value}"
                    );
                }
                break;
        }

        return line;
    }

    private void ReportResults(List<Violation> violations, ValidatorOptions options)
    {
        if (_context == null) return;

        if (!violations.Any())
        {
            _context.Logger.LogInformation("✅ No violations found");
            return;
        }

        Console.WriteLine($"\n📋 Validation Results");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        var grouped = violations.GroupBy(v => v.Severity);

        foreach (var group in grouped.OrderByDescending(g => g.Key))
        {
            Console.WriteLine($"\n{GetSeverityIcon(group.Key)} {group.Key} ({group.Count()})");
            Console.WriteLine("──────────────────────────────────────────────────────────");

            foreach (var violation in group)
            {
                Console.WriteLine($"  {violation.Code}: {violation.Message}");
                Console.WriteLine($"  📁 {Path.GetFileName(violation.File)}:{violation.Line}");
                Console.WriteLine();
            }
        }

        var errors = violations.Count(v => v.Severity == Severity.Error);
        var warnings = violations.Count(v => v.Severity == Severity.Warning);
        var info = violations.Count(v => v.Severity == Severity.Info);

        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Total: {violations.Count} | Errors: {errors} | Warnings: {warnings} | Info: {info}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }

    private string GetSeverityIcon(Severity severity)
    {
        return severity switch
        {
            Severity.Error => "❌",
            Severity.Warning => "⚠️",
            Severity.Info => "ℹ️",
            _ => "•"
        };
    }
}

internal class ValidatorOptions
{
    public string? Rule { get; set; }
    public bool All { get; set; }
    public string? Path { get; set; }
    public bool WarningsOnly { get; set; }
    public bool Fix { get; set; }
    public string? ConfigFile { get; set; }
}

internal class Violation
{
    public string Rule { get; set; } = "";
    public string File { get; set; } = "";
    public int Line { get; set; }
    public string Message { get; set; } = "";
    public Severity Severity { get; set; }
    public string Code { get; set; } = "";
    public bool Fixable { get; set; }
}

internal enum Severity
{
    Info,
    Warning,
    Error
}
