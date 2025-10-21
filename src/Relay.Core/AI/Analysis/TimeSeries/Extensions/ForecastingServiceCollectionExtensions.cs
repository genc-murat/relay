using System;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Extension methods for registering forecasting services
/// </summary>
public static class ForecastingServiceCollectionExtensions
{
    /// <summary>
    /// Adds forecasting services to the service collection
    /// </summary>
    public static IServiceCollection AddForecasting(this IServiceCollection services)
    {
        return services.AddForecasting(_ => { });
    }

    /// <summary>
    /// Adds forecasting services to the service collection with configuration
    /// </summary>
    public static IServiceCollection AddForecasting(
        this IServiceCollection services,
        Action<ForecastingConfiguration> configure)
    {
        services.Configure(configure);

        services.AddSingleton<ForecastingConfiguration>();
        services.AddSingleton<IForecastingModelManager, ForecastingModelManager>();
        services.AddSingleton<IForecastingMethodManager, ForecastingMethodManager>();
        services.AddTransient<IForecastingTrainer, ForecastingTrainer>();
        services.AddTransient<IForecastingPredictor, ForecastingPredictor>();
        services.AddTransient<IForecastingService, ForecastingService>();

        return services;
    }
}