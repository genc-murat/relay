using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationEngineTemplateTests
    {
        [Fact]
        public async Task OptimizationEngineTemplate_ShouldExecuteWorkflow()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestEngine", result.StrategyName);
            Assert.True(engine.PreOptimizationCalled);
            Assert.True(engine.PostOptimizationCalled);
            Assert.True(engine.NotifyObserversCalled);
        }

        [Fact]
        public async Task OptimizationEngineTemplate_ShouldValidateContext()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = null // Invalid context
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid optimization context", result.ErrorMessage);
        }

        [Fact]
        public async Task OptimizationEngineTemplate_ShouldHandleExecutionFailure()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new FailingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Template", result.StrategyName);
            Assert.Contains("Test execution failure", result.ErrorMessage);
        }

        [Fact]
        public async Task OptimizationEngineTemplate_ShouldNotifyObserversOnFailure()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observer = new Mock<IPerformanceObserver>();
            var observers = new List<IPerformanceObserver> { observer.Object };
            var engine = new FailingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            await engine.OptimizeAsync(context);

            // Assert
            observer.Verify(o => o.OnOptimizationCompletedAsync(It.Is<StrategyExecutionResult>(r => !r.Success)), Times.Once);
        }

        [Fact]
        public async Task OptimizationEngineTemplate_ShouldSelectStrategies()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory, observers);

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestEngine", result.StrategyName);
        }

        [Fact]
        public void OptimizationEngineTemplate_ShouldHaveVirtualMethods()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            // Act & Assert
            // Verify that the methods are virtual by checking if they can be overridden
            Assert.True(typeof(OptimizationEngineTemplate).GetMethod("OptimizeAsync")!.IsVirtual);
            Assert.True(typeof(OptimizationEngineTemplate).GetMethod("PreOptimizationAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.IsVirtual);
            Assert.True(typeof(OptimizationEngineTemplate).GetMethod("PostOptimizationAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.IsVirtual);
        }

        [Fact]
        public async Task OptimizeAsync_MeasuresExecutionTime_Correctly()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new DelayedOptimizationEngine(logger, strategyFactory.Object, observers, TimeSpan.FromMilliseconds(100));

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.ExecutionTime.TotalMilliseconds >= 100);
            Assert.NotEqual(TimeSpan.Zero, result.ExecutionTime);
        }

        [Fact]
        public async Task OptimizeAsync_PreOptimizationAsync_IsCalledBeforeExecution()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new HooksTrackingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.True(engine.PreOptimizationCalled);
            Assert.True(engine.HookCallOrder.Count > 0);
            Assert.Equal("PreOptimization", engine.HookCallOrder[0]);
            Assert.Equal("PostOptimization", engine.HookCallOrder[engine.HookCallOrder.Count - 1]);
        }

        [Fact]
        public async Task OptimizeAsync_PostOptimizationAsync_IsCalledAfterExecution()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new HooksTrackingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.True(engine.PostOptimizationCalled);
            Assert.Contains("PostOptimization", engine.HookCallOrder);
        }

        [Fact]
        public async Task OptimizeAsync_NotifiesObservers_OnSuccess()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observer1 = new Mock<IPerformanceObserver>();
            var observer2 = new Mock<IPerformanceObserver>();
            var observers = new List<IPerformanceObserver> { observer1.Object, observer2.Object };
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            observer1.Verify(o => o.OnOptimizationCompletedAsync(It.Is<StrategyExecutionResult>(r => r.Success)), Times.Once);
            observer2.Verify(o => o.OnOptimizationCompletedAsync(It.Is<StrategyExecutionResult>(r => r.Success)), Times.Once);
        }

        [Fact]
        public async Task OptimizeAsync_NotifiesMultipleObservers_EvenIfOneFails()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());

            var observer1 = new Mock<IPerformanceObserver>();
            observer1.Setup(o => o.OnOptimizationCompletedAsync(It.IsAny<StrategyExecutionResult>()))
                .ThrowsAsync(new InvalidOperationException("Observer 1 failed"));

            var observer2 = new Mock<IPerformanceObserver>();

            var observers = new List<IPerformanceObserver> { observer1.Object, observer2.Object };
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            // Both observers should be called even if first one fails
            observer1.Verify(o => o.OnOptimizationCompletedAsync(It.IsAny<StrategyExecutionResult>()), Times.Once);
            observer2.Verify(o => o.OnOptimizationCompletedAsync(It.IsAny<StrategyExecutionResult>()), Times.Once);
        }

        [Fact]
        public async Task OptimizeAsync_HandlesCancellation_Gracefully()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new CancellationAwareOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));

            // Act
            var result = await engine.OptimizeAsync(context, cts.Token);

            // Assert
            // Should handle cancellation gracefully
            Assert.NotNull(result);
        }

        [Fact]
        public async Task OptimizeAsync_ContextValidationFailure_ReturnsFailureResult()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = null // Invalid
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid optimization context", result.ErrorMessage);
        }

        [Fact]
        public async Task OptimizeAsync_StrategySelectionIsInvoked_WithCorrectContext()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new StrategyTrackingOptimizationEngine(logger, strategyFactory, observers);

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(object)
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.True(engine.SelectStrategiesCalled);
            Assert.Equal("AnalyzeRequest", engine.ContextPassed?.Operation);
        }

        [Fact]
        public async Task OptimizeAsync_ReturnsResultWithStrategyName_FromExecution()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new TestOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestEngine", result.StrategyName);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task OptimizeAsync_StoresErrorMessage_OnExecutionException()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new FailingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Test execution failure", result.ErrorMessage);
        }

        [Fact]
        public async Task OptimizeAsync_WorkflowSequence_IsCorrect()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observer = new Mock<IPerformanceObserver>();
            var observers = new List<IPerformanceObserver> { observer.Object };
            var engine = new HooksTrackingOptimizationEngine(logger, strategyFactory.Object, observers);

            var context = new OptimizationContext
            {
                Operation = "TestOperation"
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("PreOptimization", engine.HookCallOrder);
            Assert.Contains("PostOptimization", engine.HookCallOrder);
            // Verify order: PreOptimization -> Execution -> PostOptimization -> Notify
            var preIndex = engine.HookCallOrder.IndexOf("PreOptimization");
            var postIndex = engine.HookCallOrder.IndexOf("PostOptimization");
            Assert.True(preIndex < postIndex, "PreOptimization should be called before PostOptimization");
        }

        // Test implementations
        private class TestOptimizationEngine : OptimizationEngineTemplate
        {
            public bool PreOptimizationCalled { get; private set; }
            public bool PostOptimizationCalled { get; private set; }
            public bool NotifyObserversCalled { get; private set; }

            public TestOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers)
                : base(logger, strategyFactory, observers)
            {
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return !string.IsNullOrEmpty(context.Operation);
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken); // Simulate async work
                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "TestEngine",
                    Confidence = 0.8,
                    ExecutionTime = TimeSpan.FromMilliseconds(10)
                };
            }

            protected override ValueTask PreOptimizationAsync(
                OptimizationContext context,
                CancellationToken cancellationToken)
            {
                PreOptimizationCalled = true;
                return ValueTask.CompletedTask;
            }

            protected override ValueTask PostOptimizationAsync(
                StrategyExecutionResult result,
                OptimizationContext context,
                CancellationToken cancellationToken)
            {
                PostOptimizationCalled = true;
                return ValueTask.CompletedTask;
            }

            protected override ValueTask NotifyObserversAsync(
                StrategyExecutionResult result,
                CancellationToken cancellationToken)
            {
                NotifyObserversCalled = true;
                return base.NotifyObserversAsync(result, cancellationToken);
            }
        }

        private class FailingOptimizationEngine : OptimizationEngineTemplate
        {
            public FailingOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers)
                : base(logger, strategyFactory, observers)
            {
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return true;
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken);
                throw new InvalidOperationException("Test execution failure");
            }
        }

        private class DelayedOptimizationEngine : OptimizationEngineTemplate
        {
            private readonly TimeSpan _delay;

            public DelayedOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers,
                TimeSpan delay)
                : base(logger, strategyFactory, observers)
            {
                _delay = delay;
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return !string.IsNullOrEmpty(context.Operation);
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                await Task.Delay(_delay, cancellationToken);
                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "DelayedEngine",
                    Confidence = 0.9,
                    ExecutionTime = _delay
                };
            }
        }

        private class HooksTrackingOptimizationEngine : OptimizationEngineTemplate
        {
            public bool PreOptimizationCalled { get; private set; }
            public bool PostOptimizationCalled { get; private set; }
            public List<string> HookCallOrder { get; } = new();

            public HooksTrackingOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers)
                : base(logger, strategyFactory, observers)
            {
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return !string.IsNullOrEmpty(context.Operation);
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                HookCallOrder.Add("Execution");
                await Task.Delay(1, cancellationToken);
                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "HooksTrackingEngine",
                    Confidence = 0.85,
                    ExecutionTime = TimeSpan.FromMilliseconds(10)
                };
            }

            protected override ValueTask PreOptimizationAsync(
                OptimizationContext context,
                CancellationToken cancellationToken)
            {
                PreOptimizationCalled = true;
                HookCallOrder.Add("PreOptimization");
                return base.PreOptimizationAsync(context, cancellationToken);
            }

            protected override ValueTask PostOptimizationAsync(
                StrategyExecutionResult result,
                OptimizationContext context,
                CancellationToken cancellationToken)
            {
                PostOptimizationCalled = true;
                HookCallOrder.Add("PostOptimization");
                return base.PostOptimizationAsync(result, context, cancellationToken);
            }
        }

        private class CancellationAwareOptimizationEngine : OptimizationEngineTemplate
        {
            public CancellationAwareOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers)
                : base(logger, strategyFactory, observers)
            {
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return !string.IsNullOrEmpty(context.Operation);
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                try
                {
                    await Task.Delay(200, cancellationToken);
                    return new StrategyExecutionResult
                    {
                        Success = true,
                        StrategyName = "CancellationEngine",
                        Confidence = 0.8,
                        ExecutionTime = TimeSpan.FromMilliseconds(200)
                    };
                }
                catch (OperationCanceledException)
                {
                    return new StrategyExecutionResult
                    {
                        Success = false,
                        StrategyName = "CancellationEngine",
                        ErrorMessage = "Operation was cancelled",
                        ExecutionTime = TimeSpan.Zero
                    };
                }
            }
        }

        private class StrategyTrackingOptimizationEngine : OptimizationEngineTemplate
        {
            public bool SelectStrategiesCalled { get; private set; }
            public OptimizationContext? ContextPassed { get; private set; }

            public StrategyTrackingOptimizationEngine(
                ILogger logger,
                OptimizationStrategyFactory strategyFactory,
                IEnumerable<IPerformanceObserver> observers)
                : base(logger, strategyFactory, observers)
            {
            }

            protected override bool ValidateContext(OptimizationContext context)
            {
                return !string.IsNullOrEmpty(context.Operation);
            }

            protected override IEnumerable<IOptimizationStrategy> SelectStrategies(OptimizationContext context)
            {
                SelectStrategiesCalled = true;
                ContextPassed = context;
                return base.SelectStrategies(context);
            }

            protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
                OptimizationContext context,
                IEnumerable<IOptimizationStrategy> strategies,
                CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken);
                return new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyTrackingEngine",
                    Confidence = 0.9,
                    ExecutionTime = TimeSpan.FromMilliseconds(1)
                };
            }
        }
    }
}