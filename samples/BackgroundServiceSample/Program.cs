using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay;
using Relay.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddRelayConfiguration();
    services.AddHostedService<DataProcessingWorker>();
    services.AddHostedService<ReportGenerationWorker>();
    services.AddScoped<DataProcessor>();
});

var host = builder.Build();

Console.WriteLine("🚀 Relay Background Service Sample");
Console.WriteLine("=" + new string('=', 70));
Console.WriteLine("Running background workers with Relay integration...");
Console.WriteLine();

await host.RunAsync();

// Commands
public record ProcessDataCommand(string Data) : IRequest<string>;
public record GenerateReportCommand(DateTime StartDate, DateTime EndDate) : IRequest<ReportResult>;
public record ReportResult(int RecordsProcessed, TimeSpan Duration);

// Data processor service
public class DataProcessor
{
    [Handle]
    public async ValueTask<string> ProcessData(ProcessDataCommand command, CancellationToken ct)
    {
        await Task.Delay(1000, ct); // Simulate processing
        return $"Processed: {command.Data}";
    }

    [Handle]
    public async ValueTask<ReportResult> GenerateReport(GenerateReportCommand command, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        await Task.Delay(2000, ct); // Simulate report generation
        var duration = DateTime.UtcNow - start;
        return new ReportResult(Random.Shared.Next(100, 1000), duration);
    }
}

// Worker 1: Processes data every 5 seconds
public class DataProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataProcessingWorker> _logger;
    private int _executionCount = 0;

    public DataProcessingWorker(IServiceProvider serviceProvider, ILogger<DataProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 DataProcessingWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _executionCount++;

                using var scope = _serviceProvider.CreateScope();
                var relay = scope.ServiceProvider.GetRequiredService<IRelay>();

                var command = new ProcessDataCommand($"Batch_{_executionCount}_{DateTime.UtcNow:HHmmss}");
                var result = await relay.SendAsync(command, stoppingToken);

                _logger.LogInformation($"✅ DataProcessing #{_executionCount}: {result}");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DataProcessingWorker is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DataProcessingWorker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

// Worker 2: Generates reports every 10 seconds
public class ReportGenerationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportGenerationWorker> _logger;
    private int _reportCount = 0;

    public ReportGenerationWorker(IServiceProvider serviceProvider, ILogger<ReportGenerationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📊 ReportGenerationWorker started");

        // Wait a bit before starting
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _reportCount++;

                using var scope = _serviceProvider.CreateScope();
                var relay = scope.ServiceProvider.GetRequiredService<IRelay>();

                var command = new GenerateReportCommand(
                    DateTime.UtcNow.AddHours(-24),
                    DateTime.UtcNow
                );

                var result = await relay.SendAsync(command, stoppingToken);

                _logger.LogInformation(
                    $"✅ Report #{_reportCount}: {result.RecordsProcessed} records in {result.Duration.TotalSeconds:F1}s"
                );

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ReportGenerationWorker is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReportGenerationWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
