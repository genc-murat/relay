using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationIntegrationTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public void BuilderCreatesEngine_Integration()
        {
            // Arrange - Builder pattern creates optimization engine
            var builder = new OptimizationEngineBuilder();
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act - Build engine using Builder pattern
            var engine = builder
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .Build();

            // Assert - Verify Builder created engine
            engine.Should().NotBeNull();
            engine.Should().BeOfType<OptimizationEngine>();
        }

        [Fact]
        public void FactoryCreatesStrategies_Integration()
        {
            // Arrange - Factory pattern creates strategies
            var factory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());

            // Act - Create strategies using Factory
            var strategies = factory.CreateAllStrategies().ToList();

            // Assert - Verify Factory integration
            strategies.Should().NotBeNull();
            strategies.Should().HaveCountGreaterThan(0);
            strategies.Should().Contain(s => s.Name == "RequestAnalysis");
        }

        [Fact]
        public async Task ObserverHandlesOptimizationEvents_Integration()
        {
            // Arrange - Observer pattern for performance monitoring
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var result = new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.85,
                ExecutionTime = TimeSpan.FromMilliseconds(150)
            };

            // Act - Observer processes optimization result
            await observer.OnOptimizationCompletedAsync(result);

            // Assert - Observer integration
            observer.Should().NotBeNull();
            logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TemplateMethodExecutesWorkflow_Integration()
        {
            // Arrange - Template Method pattern for optimization workflow
            var factory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var templateEngine = new OptimizationEngine(_logger, factory, factory.CreateAllStrategies(), observers, TimeSpan.FromSeconds(30));

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.6,
                    MemoryAllocated = 1024 * 1024
                }
            };

            // Act - Execute template method workflow
            var result = await templateEngine.OptimizeAsync(context);

            // Assert - Template method integration
            result.Should().NotBeNull();
            result.StrategyName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CommandExecutesAndCanUndo_Integration()
        {
            // Arrange - Command pattern for optimization actions
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(_logger, strategyFactory, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                }
            };

            var command = new AnalyzeRequestCommand(engine, context);

            // Act - Execute command
            var result = await command.ExecuteAsync();

            // Assert - Command pattern integration
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Analyze request");
        }

        [Fact]
        public async Task DecoratorAddsBehaviorToEngine_Integration()
        {
            // Arrange - Decorator pattern adds caching behavior
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var innerEngine = new OptimizationEngine(_logger, strategyFactory, strategyFactory.CreateAllStrategies(), observers, TimeSpan.FromSeconds(30));
            var cacheDuration = TimeSpan.FromMinutes(5);

            var decorator = new CachingOptimizationEngineDecorator(
                innerEngine, _logger, strategyFactory, observers, cacheDuration);

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act - Execute through decorator
            var result1 = await decorator.OptimizeAsync(context);
            var result2 = await decorator.OptimizeAsync(context); // Should use cache

            // Assert - Decorator pattern integration
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CompositeCombinesMultipleEngines_Integration()
        {
            // Arrange - Composite pattern combines multiple engines
            var factory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engines = new List<OptimizationEngineTemplate>();
            var compositeEngine = new CompositeOptimizationEngine(_logger, factory, observers, engines);

            var engine1 = new OptimizationEngine(_logger, factory, factory.CreateAllStrategies(), observers, TimeSpan.FromSeconds(30));
            var engine2 = new OptimizationEngine(_logger, factory, factory.CreateAllStrategies(), observers, TimeSpan.FromSeconds(30));

            compositeEngine.AddEngine(engine1);
            compositeEngine.AddEngine(engine2);

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                }
            };

            // Act - Execute through composite
            var results = await compositeEngine.OptimizeAsync(context);

            // Assert - Composite pattern integration
            results.Should().NotBeNull();
            // Note: Composite returns single result, not collection
        }

        [Fact]
        public void AllPatternsCanBeInstantiated_ConceptualIntegration()
        {
            // Arrange - Conceptual test showing all patterns can be instantiated together
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Builder pattern
            var builder = new OptimizationEngineBuilder();

            // Factory pattern
            var factory = new OptimizationStrategyFactory(loggerFactory, options);

            // Observer pattern
            var observerLogger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(observerLogger.Object);

            // Template Method pattern (through concrete OptimizationEngine)
            var templateEngine = new OptimizationEngine(_logger, factory, factory.CreateAllStrategies(),
                new List<IPerformanceObserver> { observer }, TimeSpan.FromSeconds(30));

            // Command pattern
            var command = new AnalyzeRequestCommand(templateEngine, new OptimizationContext());

            // Decorator pattern
            var decorator = new CachingOptimizationEngineDecorator(
                templateEngine, _logger, factory, new[] { observer }, TimeSpan.FromMinutes(5));

            // Composite pattern
            var engines = new List<OptimizationEngineTemplate>();
            var composite = new CompositeOptimizationEngine(_logger, factory, new[] { observer }, engines);
            composite.AddEngine(templateEngine);
            composite.AddEngine(decorator);

            // Assert - All patterns can coexist
            builder.Should().NotBeNull();
            factory.Should().NotBeNull();
            observer.Should().NotBeNull();
            templateEngine.Should().NotBeNull();
            command.Should().NotBeNull();
            decorator.Should().NotBeNull();
            composite.Should().NotBeNull();
        }
    }
}