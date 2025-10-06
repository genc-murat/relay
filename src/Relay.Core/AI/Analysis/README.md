# AI Analysis Module Organization

This directory contains AI-driven analysis components for the Relay framework.

## Directory Structure

```
Analysis/
├── Engines/                    # Analysis engines and processors
│   ├── PatternRecognitionEngine.cs   - ML-based pattern recognition and model retraining
│   ├── PerformanceAnalyzer.cs        - Performance pattern analysis and recommendations
│   └── TrendAnalyzer.cs              - Trend detection and analysis
│
├── Models/                     # Data models and DTOs
│   ├── AnomalyDetectionResult.cs     - Anomaly detection result model
│   ├── AnomalySeverity.cs            - Anomaly severity levels enum
│   ├── ForecastResult.cs             - Forecast prediction result
│   ├── InsightSeverity.cs            - Insight severity levels enum
│   ├── MetricAnomaly.cs              - Metric anomaly model
│   ├── MetricDataPoint.cs            - Time-series data point (ML.NET compatible)
│   ├── MetricForecastResult.cs       - Metric forecast result with bounds
│   ├── MetricStatistics.cs           - Statistical summary for metrics
│   ├── MetricTrendData.cs            - Metric trend data
│   ├── MovingAverageData.cs          - Moving average calculations
│   ├── RegressionResult.cs           - Regression analysis result
│   ├── SeasonalityPattern.cs         - Seasonality pattern detection
│   ├── TrendAnalysisResult.cs        - Trend analysis result
│   ├── TrendDirection.cs             - Trend direction enum
│   └── TrendInsight.cs               - Trend insight model
│
├── TimeSeries/                 # Time-series storage and analysis
│   ├── CircularBuffer.cs             - Fixed-size circular buffer implementation
│   └── TimeSeriesDatabase.cs         - ML.NET-based time-series database
│
├── ConnectionMetricsCache.cs   - Connection metrics caching
├── ConnectionMetricsCollector.cs - Connection metrics collection
└── SystemMetricsCalculator.cs  - System-level metrics calculations

```

## Key Components

### Engines
Analysis engines that process data and generate insights:
- **PatternRecognitionEngine**: Retrains ML models based on prediction feedback
- **PerformanceAnalyzer**: Identifies bottlenecks and optimization opportunities
- **TrendAnalyzer**: Detects and analyzes trends in metrics

### Models
Data transfer objects and result models used throughout the analysis pipeline.

### TimeSeries
Time-series data storage and forecasting using ML.NET:
- **TimeSeriesDatabase**: Stores metrics, detects anomalies, and forecasts future values
- **CircularBuffer**: Efficient fixed-size storage for time-series data

## Dependencies

- Microsoft.ML (for machine learning capabilities)
- Microsoft.Extensions.Logging (for logging)

## Usage

These components are used internally by the AI optimization pipeline to:
1. Collect and analyze system metrics
2. Detect performance patterns and anomalies
3. Generate optimization recommendations
4. Forecast future resource needs
5. Retrain models based on actual results

## Testing

Comprehensive unit tests are available in:
- `tests/Relay.Core.Tests/AI/PatternRecognitionEngineTests.cs`
- `tests/Relay.Core.Tests/AI/PerformanceAnalyzerTests.cs`
- `tests/Relay.Core.Tests/AI/SystemMetricsCalculatorTests.cs`
- `tests/Relay.Core.Tests/AI/TimeSeriesDatabaseTests.cs`
