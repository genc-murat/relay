using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Training;

public class ModelMetricsTests
{
    [Fact]
    public void ModelMetrics_Should_Initialize_With_Default_Values()
    {
        // Act
        var metrics = new ModelMetrics();

        // Assert
        Assert.Null(metrics.RSquared);
        Assert.Null(metrics.MAE);
        Assert.Null(metrics.RMSE);
        Assert.Null(metrics.Accuracy);
        Assert.Null(metrics.AUC);
        Assert.Null(metrics.F1Score);
    }

    [Fact]
    public void ModelMetrics_Should_Allow_Setting_Regression_Metrics()
    {
        // Arrange
        var metrics = new ModelMetrics();

        // Act
        metrics.RSquared = 0.85;
        metrics.MAE = 0.15;
        metrics.RMSE = 0.20;

        // Assert
        Assert.Equal(0.85, metrics.RSquared);
        Assert.Equal(0.15, metrics.MAE);
        Assert.Equal(0.20, metrics.RMSE);
    }

    [Fact]
    public void ModelMetrics_Should_Allow_Setting_Classification_Metrics()
    {
        // Arrange
        var metrics = new ModelMetrics();

        // Act
        metrics.Accuracy = 0.92;
        metrics.AUC = 0.88;
        metrics.F1Score = 0.90;

        // Assert
        Assert.Equal(0.92, metrics.Accuracy);
        Assert.Equal(0.88, metrics.AUC);
        Assert.Equal(0.90, metrics.F1Score);
    }

    [Fact]
    public void ModelMetrics_Should_Support_Object_Initialization()
    {
        // Act
        var metrics = new ModelMetrics
        {
            RSquared = 0.75,
            MAE = 0.25,
            RMSE = 0.30,
            Accuracy = 0.85,
            AUC = 0.82,
            F1Score = 0.87
        };

        // Assert
        Assert.Equal(0.75, metrics.RSquared);
        Assert.Equal(0.25, metrics.MAE);
        Assert.Equal(0.30, metrics.RMSE);
        Assert.Equal(0.85, metrics.Accuracy);
        Assert.Equal(0.82, metrics.AUC);
        Assert.Equal(0.87, metrics.F1Score);
    }

    [Fact]
    public void ModelMetrics_Should_Allow_Mixed_Metrics()
    {
        // Arrange
        var metrics = new ModelMetrics();

        // Act - Set both regression and classification metrics
        metrics.RSquared = 0.80;
        metrics.Accuracy = 0.90;
        metrics.AUC = 0.85;

        // Assert
        Assert.Equal(0.80, metrics.RSquared);
        Assert.Equal(0.90, metrics.Accuracy);
        Assert.Equal(0.85, metrics.AUC);
        Assert.Null(metrics.MAE);
        Assert.Null(metrics.RMSE);
        Assert.Null(metrics.F1Score);
    }
}