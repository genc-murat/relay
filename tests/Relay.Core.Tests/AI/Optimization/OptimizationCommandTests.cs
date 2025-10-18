using System;
using System.Collections.Generic;
using System.Threading;
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
    public class OptimizationCommandTests
    {
        [Fact]
        public async Task AnalyzeRequestCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Analyze request execution patterns");
            command.CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task PredictBatchSizeCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Predict optimal batch size");
            command.CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task OptimizeCachingCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Analyze and optimize caching configuration");
            command.CanUndo.Should().BeTrue();
        }

        [Fact]
        public async Task LearnFromResultsCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Learn from past optimization results");
            command.CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task AnalyzeSystemInsightsCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            command.Description.Should().Contain("Analyze system-wide metrics");
            command.CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldExecuteCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            invoker.UndoableCommandCount.Should().Be(0); // AnalyzeRequest cannot be undone
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldTrackUndoableCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            result.Success.Should().BeTrue();
            invoker.UndoableCommandCount.Should().Be(1); // OptimizeCaching can be undone
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldUndoCommands()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new OptimizationEngine(logger, strategyFactory.Object, new List<IOptimizationStrategy>(), observers, TimeSpan.FromSeconds(30));

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
            invoker.UndoableCommandCount.Should().Be(1);

            // Act
            var undoResult = await invoker.UndoLastCommandAsync();

            // Assert
            undoResult.Should().BeFalse(); // Default implementation returns false
            invoker.UndoableCommandCount.Should().Be(0);
        }

        [Fact]
        public async Task OptimizationCommandInvoker_ShouldHandleEmptyUndoStack()
        {
            // Arrange
            var invoker = new OptimizationCommandInvoker();

            // Act
            var result = await invoker.UndoLastCommandAsync();

            // Assert
            result.Should().BeFalse();
            invoker.UndoableCommandCount.Should().Be(0);
        }

        [Fact]
        public void CommandBase_ShouldHaveCorrectProperties()
        {
            // Arrange
            var engine = new OptimizationEngine(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IOptimizationStrategy>(), new List<IPerformanceObserver>(), TimeSpan.FromSeconds(30));
            var context = new OptimizationContext { Operation = "Test" };
            var command = new TestCommand(engine, context);

            // Act & Assert
            command.Description.Should().Be("Test command");
            command.CanUndo.Should().BeFalse();
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