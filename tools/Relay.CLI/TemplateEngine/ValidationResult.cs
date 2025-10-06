namespace Relay.CLI.TemplateEngine;

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
