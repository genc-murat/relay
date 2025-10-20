using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationCommandTests
    {
        [Fact]
        public async Task AnalyzeRequestCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

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

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Analyze request execution patterns", command.Description);
            Assert.False(command.CanUndo);
        }

        [Fact]
        public async Task PredictBatchSizeCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "PredictBatchSize",
                RequestType = typeof(string),
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.5,
                    MemoryUtilization = 0.3,
                    ActiveConnections = 50
                }
            };

            var command = new PredictBatchSizeCommand(engine, context);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Predict optimal batch size", command.Description);
            Assert.False(command.CanUndo);
        }

        [Fact]
        public async Task OptimizeCachingCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "test",
                        AccessCount = 10,
                        AccessFrequency = 2.0
                    }
                }
            };

            var command = new OptimizeCachingCommand(engine, context);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Analyze and optimize caching configuration", command.Description);
            Assert.True(command.CanUndo);
        }

        [Fact]
        public async Task LearnFromResultsCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "LearnFromResults",
                AppliedStrategies = new[]
                {
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.Caching,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(50)
                    }
                }
            };

            var command = new LearnFromResultsCommand(engine, context);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Learn from past optimization results", command.Description);
            Assert.False(command.CanUndo);
        }

        [Fact]
        public async Task AnalyzeSystemInsightsCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "AnalyzeSystemInsights",
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.7,
                    MemoryUtilization = 0.5,
                    ActiveConnections = 100
                }
            };

            var command = new AnalyzeSystemInsightsCommand(engine, context);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Analyze system-wide metrics", command.Description);
            Assert.False(command.CanUndo);
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldExecuteCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                }
            };

            var command = new AnalyzeRequestCommand(engine, context);
            var invoker = new OptimizationCommandInvoker();

            // Act
            var result = await invoker.ExecuteCommandAsync(command);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, invoker.UndoableCommandCount); // AnalyzeRequest cannot be undone
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldTrackUndoableCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "test",
                        AccessCount = 10
                    }
                }
            };

            var command = new OptimizeCachingCommand(engine, context);
            var invoker = new OptimizationCommandInvoker();

            // Act
            var result = await invoker.ExecuteCommandAsync(command);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, invoker.UndoableCommandCount); // OptimizeCaching can be undone
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldUndoCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new AIOptimizationOptions();
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, options);
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(NullLoggerFactory.Instance)
                .WithOptions(options)
                .AddObservers(observers)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "test",
                        AccessCount = 10
                    }
                }
            };

            var command = new OptimizeCachingCommand(engine, context);
            var invoker = new OptimizationCommandInvoker();

            // Execute command
            await invoker.ExecuteCommandAsync(command);
            Assert.Equal(1, invoker.UndoableCommandCount);

            // Act
            var undoResult = await invoker.UndoLastCommandAsync();

            // Assert
            Assert.False(undoResult); // Default implementation returns false
            Assert.Equal(0, invoker.UndoableCommandCount);
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldHandleEmptyUndoStack()
        {
            // Arrange
            var invoker = new OptimizationCommandInvoker();

            // Act
            var result = await invoker.UndoLastCommandAsync();

            // Assert
            Assert.False(result);
            Assert.Equal(0, invoker.UndoableCommandCount);
        }

        [Fact]
        public void CommandBase_ShouldHaveCorrectProperties()
        {
            // Arrange
            var engine = new OptimizationEngine(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IOptimizationStrategy>(), new List<IPerformanceObserver>(), TimeSpan.FromSeconds(30));
            var context = new OptimizationContext { Operation = "Test" };
            var command = new TestCommand(engine, context);

            // Act & Assert
            Assert.Equal("Test command", command.Description);
            Assert.False(command.CanUndo);
        }

        // Test command implementation
        private class TestCommand : OptimizationCommandBase
        {
            public TestCommand(OptimizationEngine engine, OptimizationContext context)
                : base(engine, context)
            {
            }

            public override string Description => "Test command";

            public ValueTask<StrategyExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "Test",
                    Confidence = 0.8
                });
            }
        }
    }
}