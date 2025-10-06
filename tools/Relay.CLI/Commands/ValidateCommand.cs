using System.CommandLine;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;
using ValidationResult = Relay.CLI.Commands.Models.Validation.ValidationResult;
using ValidationStatus = Relay.CLI.Commands.Models.Validation.ValidationStatus;
using ValidationSeverity = Relay.CLI.Commands.Models.Validation.ValidationSeverity;

namespace Relay.CLI.Commands;

public static class ValidateCommand
{
    public static Command Create()
    {
        var command = new Command("validate", "Validate project structure and configuration");

        var pathOption = new Option<string>("--path", () => ".", "Project path to validate");
        var strictOption = new Option<bool>("--strict", () => false, "Use strict validation rules");
        var outputOption = new Option<string?>("--output", "Output validation report to file");
        var formatOption = new Option<string>("--format", () => "console", "Output format (console, json, markdown)");

        command.AddOption(pathOption);
        command.AddOption(strictOption);
        command.AddOption(outputOption);
        command.AddOption(formatOption);

        command.SetHandler(async (path, strict, output, format) =>
        {
            await ExecuteValidate(path, strict, output, format);
        }, pathOption, strictOption, outputOption, formatOption);

        return command;
    }

    private static async Task ExecuteValidate(string projectPath, bool strict, string? outputFile, string format)
    {
        AnsiConsole.MarkupLine("[cyan]🔍 Validating Relay project structure...[/]");
        AnsiConsole.WriteLine();
        
        var validationResults = new List<ValidationResult>();
        
        await AnsiConsole.Status()
            .StartAsync("Running validation checks...", async ctx =>
            {
                // Check 1: Project files
                ctx.Status("Checking project files...");
                await ValidateProjectFiles(projectPath, validationResults, strict);
                
                // Check 2: Handlers
                ctx.Status("Validating handlers...");
                await ValidateHandlers(projectPath, validationResults, strict);
                
                // Check 3: Requests/Responses
                ctx.Status("Validating requests and responses...");
                await ValidateRequestsAndResponses(projectPath, validationResults, strict);
                
                // Check 4: Configuration
                ctx.Status("Checking configuration...");
                await ValidateConfiguration(projectPath, validationResults, strict);
                
                // Check 5: DI Registration
                ctx.Status("Validating DI registration...");
                await ValidateDIRegistration(projectPath, validationResults, strict);
            });

        // Display results
        DisplayValidationResults(validationResults, format);

        // Save to file if requested
        if (!string.IsNullOrEmpty(outputFile))
        {
            await SaveValidationResults(validationResults, outputFile, format);
            AnsiConsole.MarkupLine($"[green]📄 Report saved to: {outputFile}[/]");
        }

        // Set exit code
        var failCount = validationResults.Count(r => r.Status == ValidationStatus.Fail);
        Environment.ExitCode = failCount > 0 ? 2 : 0;
    }

    private static async Task ValidateProjectFiles(string path, List<ValidationResult> results, bool strict)
    {
        var projectFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        if (projectFiles.Count == 0)
        {
            results.Add(new ValidationResult
            {
                Type = "Project Files",
                Status = ValidationStatus.Fail,
                Message = "No .csproj files found in the directory",
                Severity = ValidationSeverity.Critical
            });
            return;
        }

        bool hasRelay = false;
        foreach (var projFile in projectFiles)
        {
            var content = await File.ReadAllTextAsync(projFile);
            
            if (content.Contains("Relay.Core") || content.Contains("<PackageReference Include=\"Relay\""))
            {
                hasRelay = true;
                results.Add(new ValidationResult
                {
                    Type = "Package Reference",
                    Status = ValidationStatus.Pass,
                    Message = $"Relay package found in {Path.GetFileName(projFile)}",
                    Severity = ValidationSeverity.Info
                });
            }

            // Check for nullable reference types
            if (strict && !content.Contains("<Nullable>enable</Nullable>"))
            {
                results.Add(new ValidationResult
                {
                    Type = "Code Quality",
                    Status = ValidationStatus.Warning,
                    Message = $"Nullable reference types not enabled in {Path.GetFileName(projFile)}",
                    Severity = ValidationSeverity.Medium
                });
            }

            // Check for latest C# version
            if (content.Contains("<LangVersion>latest</LangVersion>"))
            {
                results.Add(new ValidationResult
                {
                    Type = "Code Quality",
                    Status = ValidationStatus.Pass,
                    Message = $"Latest C# features enabled in {Path.GetFileName(projFile)}",
                    Severity = ValidationSeverity.Info
                });
            }
        }

        if (!hasRelay)
        {
            results.Add(new ValidationResult
            {
                Type = "Package Reference",
                Status = ValidationStatus.Fail,
                Message = "No Relay package references found in any project",
                Severity = ValidationSeverity.Critical,
                Suggestion = "Add Relay.Core package: dotnet add package Relay.Core"
            });
        }
    }

    private static async Task ValidateHandlers(string path, List<ValidationResult> results, bool strict)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        int handlerCount = 0;
        int validHandlers = 0;
        int handlersWithIssues = 0;

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            
            if (content.Contains("IRequestHandler") || content.Contains("INotificationHandler"))
            {
                handlerCount++;
                var tree = CSharpSyntaxTree.ParseText(content);
                var root = await tree.GetRootAsync();
                
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                
                foreach (var classDecl in classes)
                {
                    var className = classDecl.Identifier.Text;
                    bool hasHandleMethod = false;
                    bool usesValueTask = false;
                    bool hasCancellationToken = false;

                    var methods = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    
                    foreach (var method in methods)
                    {
                        if (method.Identifier.Text.Contains("Handle"))
                        {
                            hasHandleMethod = true;
                            
                            // Check return type
                            var returnType = method.ReturnType.ToString();
                            usesValueTask = returnType.Contains("ValueTask");
                            
                            // Check for CancellationToken
                            hasCancellationToken = method.ParameterList.Parameters
                                .Any(p => p.Type?.ToString().Contains("CancellationToken") ?? false);
                        }
                    }

                    if (hasHandleMethod)
                    {
                        if (usesValueTask && hasCancellationToken)
                        {
                            validHandlers++;
                        }
                        else
                        {
                            handlersWithIssues++;
                            
                            if (!usesValueTask)
                            {
                                results.Add(new ValidationResult
                                {
                                    Type = "Handler Pattern",
                                    Status = ValidationStatus.Warning,
                                    Message = $"{className} in {Path.GetFileName(file)} uses Task instead of ValueTask",
                                    Severity = ValidationSeverity.Medium,
                                    Suggestion = "Consider using ValueTask<T> for better performance"
                                });
                            }
                            
                            if (!hasCancellationToken)
                            {
                                results.Add(new ValidationResult
                                {
                                    Type = "Handler Pattern",
                                    Status = ValidationStatus.Warning,
                                    Message = $"{className} in {Path.GetFileName(file)} missing CancellationToken parameter",
                                    Severity = ValidationSeverity.High,
                                    Suggestion = "Add CancellationToken parameter to support cancellation"
                                });
                            }
                        }
                    }
                }
            }
        }

        if (handlerCount > 0)
        {
            results.Add(new ValidationResult
            {
                Type = "Handlers",
                Status = ValidationStatus.Pass,
                Message = $"Found {handlerCount} handler(s): {validHandlers} optimal, {handlersWithIssues} with suggestions",
                Severity = ValidationSeverity.Info
            });
        }
        else
        {
            results.Add(new ValidationResult
            {
                Type = "Handlers",
                Status = strict ? ValidationStatus.Fail : ValidationStatus.Warning,
                Message = "No handlers found in the project",
                Severity = strict ? ValidationSeverity.High : ValidationSeverity.Low,
                Suggestion = "Use 'relay scaffold' to create handlers"
            });
        }
    }

    private static async Task ValidateRequestsAndResponses(string path, List<ValidationResult> results, bool strict)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        int requestCount = 0;
        int recordRequests = 0;
        int classRequests = 0;

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            
            if (content.Contains(": IRequest") || content.Contains(": INotification"))
            {
                requestCount++;
                
                if (content.Contains("public record"))
                {
                    recordRequests++;
                }
                else if (content.Contains("public class"))
                {
                    classRequests++;
                    
                    if (strict)
                    {
                        results.Add(new ValidationResult
                        {
                            Type = "Request Pattern",
                            Status = ValidationStatus.Warning,
                            Message = $"Request in {Path.GetFileName(file)} uses class instead of record",
                            Severity = ValidationSeverity.Low,
                            Suggestion = "Consider using 'record' for immutable request objects"
                        });
                    }
                }
            }
        }

        if (requestCount > 0)
        {
            results.Add(new ValidationResult
            {
                Type = "Requests",
                Status = ValidationStatus.Pass,
                Message = $"Found {requestCount} request(s): {recordRequests} record(s), {classRequests} class(es)",
                Severity = ValidationSeverity.Info
            });
        }
    }

    private static async Task ValidateConfiguration(string path, List<ValidationResult> results, bool strict)
    {
        // Check for appsettings.json
        var appSettingsPath = Path.Combine(path, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            results.Add(new ValidationResult
            {
                Type = "Configuration",
                Status = ValidationStatus.Pass,
                Message = "appsettings.json found",
                Severity = ValidationSeverity.Info
            });
        }

        // Check for .relay-cli.json
        var cliConfigPath = Path.Combine(path, ".relay-cli.json");
        if (File.Exists(cliConfigPath))
        {
            try
            {
                var configJson = await File.ReadAllTextAsync(cliConfigPath);
                var config = JsonSerializer.Deserialize<JsonElement>(configJson);
                
                results.Add(new ValidationResult
                {
                    Type = "Configuration",
                    Status = ValidationStatus.Pass,
                    Message = "Relay CLI configuration found and valid",
                    Severity = ValidationSeverity.Info
                });
            }
            catch (JsonException)
            {
                results.Add(new ValidationResult
                {
                    Type = "Configuration",
                    Status = ValidationStatus.Fail,
                    Message = ".relay-cli.json is not valid JSON",
                    Severity = ValidationSeverity.Medium,
                    Suggestion = "Fix JSON syntax errors in configuration file"
                });
            }
        }
    }

    private static async Task ValidateDIRegistration(string path, List<ValidationResult> results, bool strict)
    {
        var csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        bool hasAddRelay = false;

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            
            if (content.Contains("AddRelay()") || content.Contains(".AddRelay("))
            {
                hasAddRelay = true;
                results.Add(new ValidationResult
                {
                    Type = "DI Registration",
                    Status = ValidationStatus.Pass,
                    Message = $"AddRelay() registration found in {Path.GetFileName(file)}",
                    Severity = ValidationSeverity.Info
                });
                break;
            }
        }

        if (!hasAddRelay)
        {
            results.Add(new ValidationResult
            {
                Type = "DI Registration",
                Status = strict ? ValidationStatus.Fail : ValidationStatus.Warning,
                Message = "No AddRelay() registration found",
                Severity = strict ? ValidationSeverity.High : ValidationSeverity.Medium,
                Suggestion = "Add services.AddRelay() in your DI configuration"
            });
        }
    }

    private static void DisplayValidationResults(List<ValidationResult> results, string format)
    {
        if (format == "console")
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("[bold]Type[/]");
            table.AddColumn("[bold]Status[/]");
            table.AddColumn("[bold]Message[/]");

            foreach (var result in results)
            {
                var statusColor = result.Status switch
                {
                    ValidationStatus.Pass => "green",
                    ValidationStatus.Warning => "yellow",
                    ValidationStatus.Fail => "red",
                    _ => "white"
                };

                var statusText = result.Status switch
                {
                    ValidationStatus.Pass => "✅ Pass",
                    ValidationStatus.Warning => "⚠️  Warn",
                    ValidationStatus.Fail => "❌ Fail",
                    _ => "❓"
                };

                table.AddRow(
                    result.Type,
                    $"[{statusColor}]{statusText}[/]",
                    result.Message + (result.Suggestion != null ? $"\n[dim]💡 {result.Suggestion}[/]" : "")
                );
            }

            AnsiConsole.Write(table);

            // Summary
            AnsiConsole.WriteLine();
            var passCount = results.Count(r => r.Status == ValidationStatus.Pass);
            var warnCount = results.Count(r => r.Status == ValidationStatus.Warning);
            var failCount = results.Count(r => r.Status == ValidationStatus.Fail);

            var summary = new Panel($@"[green]✅ Passed:[/] {passCount}
[yellow]⚠️  Warnings:[/] {warnCount}
[red]❌ Failed:[/] {failCount}")
                .Header("[bold]Validation Summary[/]")
                .BorderColor(failCount > 0 ? Color.Red : (warnCount > 0 ? Color.Yellow : Color.Green));

            AnsiConsole.Write(summary);
        }
    }

    private static async Task SaveValidationResults(List<ValidationResult> results, string outputFile, string format)
    {
        if (format == "json")
        {
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputFile, json);
        }
        else if (format == "markdown")
        {
            var md = new System.Text.StringBuilder();
            md.AppendLine("# Validation Report");
            md.AppendLine();
            md.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine();
            md.AppendLine("## Results");
            md.AppendLine();
            
            foreach (var result in results)
            {
                var icon = result.Status switch
                {
                    ValidationStatus.Pass => "✅",
                    ValidationStatus.Warning => "⚠️",
                    ValidationStatus.Fail => "❌",
                    _ => "❓"
                };
                
                md.AppendLine($"### {icon} {result.Type}");
                md.AppendLine($"**Status:** {result.Status}");
                md.AppendLine($"**Message:** {result.Message}");
                if (result.Suggestion != null)
                {
                    md.AppendLine($"**Suggestion:** {result.Suggestion}");
                }
                md.AppendLine();
            }
            
            await File.WriteAllTextAsync(outputFile, md.ToString());
        }
    }
}
