using System.CommandLine;
using Spectre.Console;

namespace Relay.CLI.Commands;

public static class ValidateCommand
{
    public static Command Create()
    {
        var command = new Command("validate", "Validate project structure and configuration");

        var pathOption = new Option<string>("--path", () => ".", "Project path to validate");
        var strictOption = new Option<bool>("--strict", () => false, "Use strict validation rules");

        command.AddOption(pathOption);
        command.AddOption(strictOption);

        command.SetHandler(async (path, strict) =>
        {
            await ExecuteValidate(path, strict);
        }, pathOption, strictOption);

        return command;
    }

    private static async Task ExecuteValidate(string projectPath, bool strict)
    {
        AnsiConsole.MarkupLine("[cyan]🔍 Validating Relay project structure...[/]");
        
        var validationResults = new List<ValidationResult>();
        
        // Check for required files
        var hasRelay = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories)
            .Any(f => File.ReadAllText(f).Contains("Relay"));
            
        if (hasRelay)
        {
            validationResults.Add(new ValidationResult
            {
                Type = "Package Reference",
                Status = "✅ Pass",
                Message = "Relay package reference found"
            });
        }
        else
        {
            validationResults.Add(new ValidationResult
            {
                Type = "Package Reference", 
                Status = "❌ Fail",
                Message = "No Relay package reference found"
            });
        }

        // Display results
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Validation Type");
        table.AddColumn("Status");
        table.AddColumn("Message");

        foreach (var result in validationResults)
        {
            table.AddRow(result.Type, result.Status, result.Message);
        }

        AnsiConsole.Write(table);
        await Task.CompletedTask;
    }
}

public class ValidationResult
{
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}