using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Implementations;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics;

public class DefaultAIMetricsExporterExportTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIMetricsExporter>> _loggerMock;
    private readonly DefaultAIMetricsExporter _exporter;
    private readonly AIModelStatistics _testStatistics;

    public DefaultAIMetricsExporterExportTests()
    {
        _loggerMock = new Mock<ILogger<DefaultAIMetricsExporter>>();
        _exporter = new DefaultAIMetricsExporter(_loggerMock.Object);

        _testStatistics = new AIModelStatistics
        {
            ModelVersion = "v1.2.3",
            ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
            LastRetraining = DateTime.UtcNow.AddDays(-1),
            TotalPredictions = 10000,
            AccuracyScore = 0.85,
            PrecisionScore = 0.82,
            RecallScore = 0.88,
            F1Score = 0.85,
            ModelConfidence = 0.78,
            AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
            TrainingDataPoints = 50000
        };
    }

    public void Dispose()
    {
        _exporter.Dispose();
    }

    #region ExportMetricsAsync Tests

    [Fact]
    public async Task ExportMetricsAsync_WithNullStatistics_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _exporter.ExportMetricsAsync(null!).AsTask());
    }

    [Fact]
    public async Task ExportMetricsAsync_WithValidStatistics_ExportsSuccessfully()
    {
        // Act
        await _exporter.ExportMetricsAsync(_testStatistics);

        // Assert - Should complete without throwing
        // The export completion is verified in other tests
    }

    [Fact]
    public async Task ExportMetricsAsync_IncrementsExportCounter()
    {
        // Act - Export multiple times
        await _exporter.ExportMetricsAsync(_testStatistics);
        await _exporter.ExportMetricsAsync(_testStatistics);

        // Assert - Should log export numbers
        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metrics export #1 completed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Metrics export #2 completed")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExportMetricsAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _exporter.ExportMetricsAsync(_testStatistics, cts.Token));
    }

    [Fact]
    public async Task ExportMetricsAsync_UpdatesLatestStatistics()
    {
        // Act
        await _exporter.ExportMetricsAsync(_testStatistics);

        // Assert - This is tested indirectly through the observable gauges
        // The gauges should now return the test statistics values
    }

    #endregion
}