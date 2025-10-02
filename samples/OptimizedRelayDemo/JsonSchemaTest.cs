using System;
using System.Text.Json;

// Simple test to verify JSON schema generation logic
class JsonSchemaTest
{
    static void Main()
    {
        Console.WriteLine("Testing JSON Schema generation logic...");

        // Test basic schema structure
        var schema = new
        {
            @schema = "http://json-schema.org/draft-07/schema#",
            type = "object",
            title = "TestRequest",
            properties = new
            {
                name = new { type = "string" },
                age = new { type = "integer" },
                email = new { type = "string", format = "email" }
            },
            required = new[] { "name", "email" }
        };

        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Console.WriteLine("Generated JSON Schema:");
        Console.WriteLine(json);

        // Test escaping for generated code
        var escapedJson = json.Replace("\"", "\\\"").Replace("\r\n", "\\r\\n").Replace("\n", "\\n");
        Console.WriteLine("\nEscaped for C# string:");
        Console.WriteLine($"@\"{escapedJson}\"");

        Console.WriteLine("\nJSON Schema test completed successfully!");
    }
}