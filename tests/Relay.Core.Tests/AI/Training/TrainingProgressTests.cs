using System;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Training;

public class TrainingProgressTests
{
    [Fact]
    public void TrainingProgress_Should_Initialize_With_Default_Values()
    {
        // Act
        var progress = new TrainingProgress();

        // Assert
        Assert.Equal(TrainingPhase.Validation, progress.Phase);
        Assert.Equal(0.0, progress.ProgressPercentage);
        Assert.Equal(string.Empty, progress.StatusMessage);
        Assert.Equal(0, progress.SamplesProcessed);
        Assert.Equal(0, progress.TotalSamples);
        Assert.Equal(TimeSpan.Zero, progress.ElapsedTime);
        Assert.Null(progress.CurrentMetrics);
    }

    [Fact]
    public void TrainingProgress_Should_Allow_Setting_All_Properties()
    {
        // Arrange
        var progress = new TrainingProgress();
        var elapsedTime = TimeSpan.FromMinutes(5);
        var metrics = new ModelMetrics { Accuracy = 0.95 };

        // Act
        progress.Phase = TrainingPhase.Completed;
        progress.ProgressPercentage = 100.0;
        progress.StatusMessage = "Training completed successfully";
        progress.SamplesProcessed = 150;
        progress.TotalSamples = 150;
        progress.ElapsedTime = elapsedTime;
        progress.CurrentMetrics = metrics;

        // Assert
        Assert.Equal(TrainingPhase.Completed, progress.Phase);
        Assert.Equal(100.0, progress.ProgressPercentage);
        Assert.Equal("Training completed successfully", progress.StatusMessage);
        Assert.Equal(150, progress.SamplesProcessed);
        Assert.Equal(150, progress.TotalSamples);
        Assert.Equal(elapsedTime, progress.ElapsedTime);
        Assert.NotNull(progress.CurrentMetrics);
        Assert.Equal(0.95, progress.CurrentMetrics.Accuracy);
    }

    [Fact]
    public void TrainingProgress_Should_Support_Object_Initialization()
    {
        // Arrange
        var elapsedTime = TimeSpan.FromSeconds(30);
        var metrics = new ModelMetrics { RSquared = 0.85, MAE = 0.12 };

        // Act
        var progress = new TrainingProgress
        {
            Phase = TrainingPhase.Forecasting,
            ProgressPercentage = 75.5,
            StatusMessage = "Training forecasting models...",
            SamplesProcessed = 75,
            TotalSamples = 100,
            ElapsedTime = elapsedTime,
            CurrentMetrics = metrics
        };

        // Assert
        Assert.Equal(TrainingPhase.Forecasting, progress.Phase);
        Assert.Equal(75.5, progress.ProgressPercentage);
        Assert.Equal("Training forecasting models...", progress.StatusMessage);
        Assert.Equal(75, progress.SamplesProcessed);
        Assert.Equal(100, progress.TotalSamples);
        Assert.Equal(elapsedTime, progress.ElapsedTime);
        Assert.NotNull(progress.CurrentMetrics);
        Assert.Equal(0.85, progress.CurrentMetrics.RSquared);
        Assert.Equal(0.12, progress.CurrentMetrics.MAE);
    }

    [Fact]
    public void TrainingProgress_CurrentMetrics_Should_Be_Nullable()
    {
        // Arrange
        var progress = new TrainingProgress();

        // Act & Assert - Should be null by default
        Assert.Null(progress.CurrentMetrics);

        // Act - Set to null explicitly
        progress.CurrentMetrics = null;

        // Assert
        Assert.Null(progress.CurrentMetrics);
    }

    [Fact]
    public void TrainingProgress_CurrentMetrics_Should_Accept_Complete_ModelMetrics()
    {
        // Arrange
        var progress = new TrainingProgress();
        var metrics = new ModelMetrics
        {
            RSquared = 0.88,
            MAE = 0.10,
            RMSE = 0.15,
            Accuracy = 0.92,
            AUC = 0.89,
            F1Score = 0.91
        };

        // Act
        progress.CurrentMetrics = metrics;

        // Assert
        Assert.NotNull(progress.CurrentMetrics);
        Assert.Equal(0.88, progress.CurrentMetrics.RSquared);
        Assert.Equal(0.10, progress.CurrentMetrics.MAE);
        Assert.Equal(0.15, progress.CurrentMetrics.RMSE);
        Assert.Equal(0.92, progress.CurrentMetrics.Accuracy);
        Assert.Equal(0.89, progress.CurrentMetrics.AUC);
        Assert.Equal(0.91, progress.CurrentMetrics.F1Score);
    }
}