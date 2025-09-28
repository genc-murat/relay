using RelayDemo;

class Program
{
    static async Task Main(string[] args)
    {
        await SimpleDemo.RunAsync();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
