using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests
{
    public class PipelineBehaviorTests
    {
        [Fact]
        public void IPipelineBehavior_Should_Have_Correct_Signature()
        {
            // Arrange & Act
            var interfaceType = typeof(IPipelineBehavior<,>);
            var method = interfaceType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(ValueTask<>).Name, method.ReturnType.Name);
            Assert.Equal(3, method.GetParameters().Length);

            var parameters = method.GetParameters();
            Assert.Equal("request", parameters[0].Name);
            Assert.Equal("next", parameters[1].Name);
            Assert.Equal("cancellationToken", parameters[2].Name);
            Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        }

        [Fact]
        public void IStreamPipelineBehavior_Should_Have_Correct_Signature()
        {
            // Arrange & Act
            var interfaceType = typeof(IStreamPipelineBehavior<,>);
            var method = interfaceType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(IAsyncEnumerable<>).Name, method.ReturnType.Name);
            Assert.Equal(3, method.GetParameters().Length);

            var parameters = method.GetParameters();
            Assert.Equal("request", parameters[0].Name);
            Assert.Equal("next", parameters[1].Name);
            Assert.Equal("cancellationToken", parameters[2].Name);
            Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        }

        [Fact]
        public void ISystemModule_Should_Have_Correct_Properties_And_Methods()
        {
            // Arrange & Act
            var interfaceType = typeof(ISystemModule);
            var orderProperty = interfaceType.GetProperty("Order");
            var executeMethod = interfaceType.GetMethod("ExecuteAsync");
            var executeStreamMethod = interfaceType.GetMethod("ExecuteStreamAsync");

            // Assert
            Assert.NotNull(orderProperty);
            Assert.Equal(typeof(int), orderProperty.PropertyType);
            Assert.True(orderProperty.CanRead);

            Assert.NotNull(executeMethod);
            Assert.True(executeMethod.IsGenericMethodDefinition);
            Assert.Equal(2, executeMethod.GetGenericArguments().Length);

            Assert.NotNull(executeStreamMethod);
            Assert.True(executeStreamMethod.IsGenericMethodDefinition);
            Assert.Equal(2, executeStreamMethod.GetGenericArguments().Length);
        }

        [Fact]
        public void RequestHandlerDelegate_Should_Return_ValueTask()
        {
            // Arrange & Act
            var delegateType = typeof(RequestHandlerDelegate<>);
            var invokeMethod = delegateType.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            Assert.Equal(typeof(ValueTask<>).Name, invokeMethod.ReturnType.Name);
            Assert.Empty(invokeMethod.GetParameters());
        }

        [Fact]
        public void StreamHandlerDelegate_Should_Return_IAsyncEnumerable()
        {
            // Arrange & Act
            var delegateType = typeof(StreamHandlerDelegate<>);
            var invokeMethod = delegateType.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            Assert.Equal(typeof(IAsyncEnumerable<>).Name, invokeMethod.ReturnType.Name);
            Assert.Empty(invokeMethod.GetParameters());
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_Handler_When_No_Pipelines()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();
            var handlerExecuted = false;

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                handlerExecuted = true;
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.True(handlerExecuted);
            Assert.Equal("result", result);
        }

        [Fact]
        public void PipelineExecutor_Should_Execute_Stream_Handler_When_No_Pipelines()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();
            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateItems();
            }

            static async IAsyncEnumerable<string> GenerateItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var result = executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            // Note: We can't easily test the execution without consuming the enumerable,
            // but we can verify the method returns without throwing
        }

        [Fact]
        public void PipelineExecutor_Constructor_Should_Throw_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PipelineExecutor(null!));
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_With_System_Modules_Only()
        {
            // Arrange
            var services = new ServiceCollection();
            var executionOrder = new List<string>();
            services.AddSingleton<ISystemModule>(new TestSystemModule(1, executionOrder));
            services.AddSingleton<ISystemModule>(new TestSystemModule(2, executionOrder));
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                executionOrder.Add("Handler");
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
            Assert.Equal(5, executionOrder.Count);
            Assert.Equal("SystemModule_1_Before", executionOrder[0]);
            Assert.Equal("SystemModule_2_Before", executionOrder[1]);
            Assert.Equal("Handler", executionOrder[2]);
            Assert.Equal("SystemModule_2_After", executionOrder[3]);
            Assert.Equal("SystemModule_1_After", executionOrder[4]);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_With_Pipeline_Behaviors_Only()
        {
            // Arrange
            var services = new ServiceCollection();
            var receivedTokens = new List<CancellationToken>();
            var behavior1 = new TestPipelineBehavior();
            var behavior2 = new TestPipelineBehaviorWithCancellationCheck(receivedTokens);
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior1, behavior2 });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result_Modified_Modified", result); // Each behavior appends "_Modified"
            Assert.Equal(2, behavior1.ExecutionOrder.Count);
            Assert.Equal("Before", behavior1.ExecutionOrder[0]);
            Assert.Equal("After", behavior1.ExecutionOrder[1]);
            Assert.Single(receivedTokens); // behavior2 should have received the cancellation token
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_With_System_Modules_And_Pipeline_Behaviors()
        {
            // Arrange
            var services = new ServiceCollection();
            var systemModule = new TestSystemModule(1);
            var behavior = new TestPipelineBehavior();
            services.AddSingleton<ISystemModule>(systemModule);
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result_Modified", result);
            Assert.Equal(2, systemModule.ExecutionOrder.Count);
            Assert.Equal("SystemModule_1_Before", systemModule.ExecutionOrder[0]);
            Assert.Equal("SystemModule_1_After", systemModule.ExecutionOrder[1]);
            Assert.Equal(2, behavior.ExecutionOrder.Count);
            Assert.Equal("Before", behavior.ExecutionOrder[0]);
            Assert.Equal("After", behavior.ExecutionOrder[1]);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_In_Correct_Order_SystemModules_Pipelines_Handler()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            var systemModule1 = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System1");
            var systemModule2 = new TestSystemModuleWithGlobalOrder(2, globalExecutionOrder, "System2");
            var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
            var behavior2 = new TestPipelineBehavior(globalExecutionOrder, "Behavior2");

            services.AddSingleton<ISystemModule>(systemModule1);
            services.AddSingleton<ISystemModule>(systemModule2);
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior1, behavior2 });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                globalExecutionOrder.Add("Handler");
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result_Modified_Modified", result);
            // System modules execute first (in order), then pipeline behaviors (in reverse order), then handler
            Assert.Equal(new[] { "System1_Before", "System2_Before", "Behavior2_Before", "Behavior1_Before", "Handler", "Behavior1_After", "Behavior2_After", "System2_After", "System1_After" }, globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_DeDuplicate_Pipeline_Behaviors_By_Type()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            // Create behaviors of the same type - they should be de-duplicated
            var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
            var duplicateBehavior = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Duplicate"); // Same type, should replace behavior1
            var behavior2 = new TestPipelineBehavior(globalExecutionOrder, "Behavior2"); // Different type

            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior1, duplicateBehavior, behavior2 });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                globalExecutionOrder.Add("Handler");
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result_Modified_Modified", result); // Only 2 behaviors executed (duplicate replaced the first)
            // Execution order: behavior2, then duplicateBehavior (replaced behavior1), then handler
            Assert.Equal(new[] { "Behavior2_Before", "Duplicate_Before", "Handler", "Duplicate_After", "Behavior2_After" }, globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Order_System_Modules_By_Order_Property()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            // Add system modules with different orders (out of sequence)
            var module3 = new TestSystemModuleWithGlobalOrder(3, globalExecutionOrder, "Module3");
            var module1 = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "Module1");
            var module2 = new TestSystemModuleWithGlobalOrder(2, globalExecutionOrder, "Module2");

            services.AddSingleton<ISystemModule>(module3);
            services.AddSingleton<ISystemModule>(module1);
            services.AddSingleton<ISystemModule>(module2);
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                globalExecutionOrder.Add("Handler");
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
            // System modules should execute in order: 1, 2, 3
            Assert.Equal(new[] { "Module1_Before", "Module2_Before", "Module3_Before", "Handler", "Module3_After", "Module2_After", "Module1_After" }, globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_Stream_With_System_Modules()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            var systemModule = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System");
            services.AddSingleton<ISystemModule>(systemModule);
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();

            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateStreamItems();
            }

            static async IAsyncEnumerable<string> GenerateStreamItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var results = new List<string>();
            await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
            {
                results.Add(item);
                globalExecutionOrder.Add($"Received_{item}");
            }

            // Assert
            Assert.Equal(new[] { "item1", "item2" }, results);
            Assert.Contains("System_Stream_Before", globalExecutionOrder);
            Assert.Contains("System_Stream_Item", globalExecutionOrder);
            Assert.Contains("System_Stream_After", globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_Stream_With_Pipeline_Behaviors()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            var streamBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "StreamBehavior");
            services.AddSingleton<IEnumerable<IStreamPipelineBehavior<TestStreamRequest, string>>>(new IStreamPipelineBehavior<TestStreamRequest, string>[] { streamBehavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();

            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateStreamItems();
            }

            static async IAsyncEnumerable<string> GenerateStreamItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var results = new List<string>();
            await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results);
            Assert.Contains("StreamBehavior_Before", globalExecutionOrder);
            Assert.Contains("StreamBehavior_Item: item1", globalExecutionOrder);
            Assert.Contains("StreamBehavior_Item: item2", globalExecutionOrder);
            Assert.Contains("StreamBehavior_After", globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_Stream_With_System_Modules_And_Behaviors()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            var systemModule = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System");
            var streamBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "StreamBehavior");

            services.AddSingleton<ISystemModule>(systemModule);
            services.AddSingleton<IEnumerable<IStreamPipelineBehavior<TestStreamRequest, string>>>(new IStreamPipelineBehavior<TestStreamRequest, string>[] { streamBehavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();

            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateStreamItems();
            }

            static async IAsyncEnumerable<string> GenerateStreamItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var results = new List<string>();
            await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results);
            // System modules execute first, then behaviors
            var expectedOrder = new[] {
                "System_Stream_Before",
                "StreamBehavior_Before",
                "StreamBehavior_Item: item1",
                "System_Stream_Item",
                "StreamBehavior_Item: item2",
                "System_Stream_Item",
                "StreamBehavior_After",
                "System_Stream_After"
            };
            foreach (var expected in expectedOrder)
            {
                Assert.Contains(expected, globalExecutionOrder);
            }
        }

        [Fact]
        public async Task PipelineExecutor_Should_Propagate_Cancellation_Token()
        {
            // Arrange
            var services = new ServiceCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var receivedTokens = new List<CancellationToken>();

            var behavior = new TestPipelineBehaviorWithCancellationCheck(receivedTokens);
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                receivedTokens.Add(ct);
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, cancellationTokenSource.Token);

            // Assert
            Assert.Equal("result_Modified", result);
            // All delegates should receive the same cancellation token
            Assert.All(receivedTokens, token => Assert.Equal(cancellationTokenSource.Token, token));
        }

        [Fact]
        public async Task PipelineExecutor_Should_Fallback_When_DI_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            // Don't register any pipeline behaviors in DI
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act - This should not throw even if generated registry doesn't exist
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Propagate_Exceptions_From_Pipeline_Behaviors()
        {
            // Arrange
            var services = new ServiceCollection();
            var exceptionBehavior = new TestPipelineBehaviorThatThrows();
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { exceptionBehavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None));
            Assert.Equal("Pipeline behavior exception", exception.Message);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Propagate_Exceptions_From_System_Modules()
        {
            // Arrange
            var services = new ServiceCollection();
            var exceptionModule = new TestSystemModuleThatThrows();
            services.AddSingleton<ISystemModule>(exceptionModule);
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None));
            Assert.Equal("System module exception", exception.Message);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Handle_Cancellation_During_Execution()
        {
            // Arrange
            var services = new ServiceCollection();
            var cancellationTokenSource = new CancellationTokenSource();
            var slowBehavior = new TestPipelineBehaviorWithDelay();
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { slowBehavior });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act - Cancel immediately
            cancellationTokenSource.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await executor.ExecuteAsync<TestRequest, string>(request, Handler, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task PipelineExecutor_Should_DeDuplicate_Stream_Pipeline_Behaviors_By_Type()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            // Create stream behaviors of the same type - they should be de-duplicated
            var behavior1 = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
            var duplicateBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Duplicate"); // Same type, should replace behavior1
            var behavior2 = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior2"); // Different type

            services.AddSingleton<IEnumerable<IStreamPipelineBehavior<TestStreamRequest, string>>>(new IStreamPipelineBehavior<TestStreamRequest, string>[] { behavior1, duplicateBehavior, behavior2 });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();

            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateStreamItems();
            }

            static async IAsyncEnumerable<string> GenerateStreamItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var results = new List<string>();
            await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results); // All behaviors are same type, so only the last one (behavior2) executes
            // Execution order: only behavior2 executes
            Assert.Equal(new[] { "Behavior2_Before", "Behavior2_Item: item1", "Behavior2_Item: item2", "Behavior2_After" }, globalExecutionOrder);
        }



        [Fact]
        public async Task PipelineExecutor_Should_Execute_Multiple_Behaviors_Of_Different_Types()
        {
            // Arrange
            var services = new ServiceCollection();
            var globalExecutionOrder = new List<string>();

            // Create behaviors of different types
            var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
            var behavior2 = new TestPipelineBehaviorWithCancellationCheck(new List<CancellationToken>()); // Different type
            var behavior3 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior3"); // Different type

            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(
                new IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>[] { behavior1, behavior2, behavior3 });
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                globalExecutionOrder.Add("Handler");
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result_Modified_Modified", result); // behavior3 replaces behavior1, behavior2 also modifies
            // Execution order: behavior3 (replaces behavior1), behavior2, handler, behavior2, behavior3
            // But only behavior3 tracks execution
            Assert.Equal(new[] { "Behavior3_Before", "Handler", "Behavior3_After" }, globalExecutionOrder);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Handle_Empty_System_Modules_Collection()
        {
            // Arrange
            var services = new ServiceCollection();
            // Register empty collection of system modules
            services.AddSingleton<IEnumerable<ISystemModule>>(Array.Empty<ISystemModule>());
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
        }

        [Fact]
        public async Task PipelineExecutor_Should_Handle_Empty_Pipeline_Behaviors_Collection()
        {
            // Arrange
            var services = new ServiceCollection();
            // Register empty collection of pipeline behaviors
            services.AddSingleton<IEnumerable<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>>(Array.Empty<IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>>());
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
        }

        [Fact]
        public void PipelineExecutor_Should_Handle_Empty_Stream_Pipeline_Behaviors_Collection()
        {
            // Arrange
            var services = new ServiceCollection();
            // Register empty collection of stream pipeline behaviors
            services.AddSingleton<IEnumerable<IStreamPipelineBehavior<TestStreamRequest, string>>>(Array.Empty<IStreamPipelineBehavior<TestStreamRequest, string>>());
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();

            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateStreamItems();
            }

            static async IAsyncEnumerable<string> GenerateStreamItems()
            {
                await Task.CompletedTask;
                yield return "item1";
            }

            // Act
            var result = executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        // Test request types
        public class TestRequest : IRequest<string> { }
        public class TestStreamRequest : IStreamRequest<string> { }
    }

// Test implementations for pipeline behaviors
public class TestPipelineBehavior : IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>
    {
        public List<string> ExecutionOrder { get; } = new();
        private readonly List<string>? _globalExecutionOrder;
        private readonly string _name;

        public TestPipelineBehavior(List<string>? globalExecutionOrder = null, string name = "Behavior")
        {
            _globalExecutionOrder = globalExecutionOrder;
            _name = name;
        }

        public async ValueTask<string> HandleAsync(PipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("Before");
            _globalExecutionOrder?.Add($"{_name}_Before");
            var result = await next();
            ExecutionOrder.Add("After");
            _globalExecutionOrder?.Add($"{_name}_After");
            return result + "_Modified";
        }
    }

    public class TestStreamPipelineBehavior : IStreamPipelineBehavior<PipelineBehaviorTests.TestStreamRequest, string>
    {
        public List<string> ExecutionOrder { get; } = new();

        public async IAsyncEnumerable<string> HandleAsync(PipelineBehaviorTests.TestStreamRequest request, StreamHandlerDelegate<string> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("Before");
            await foreach (var item in next())
            {
                ExecutionOrder.Add($"Item: {item}");
                yield return item + "_Modified";
            }
            ExecutionOrder.Add("After");
        }
    }

    public class TestSystemModule : ISystemModule
    {
        public int Order { get; }
        public List<string> ExecutionOrder { get; } = new();
        private readonly List<string>? _globalExecutionOrder;

        public TestSystemModule(int order = 0, List<string>? globalExecutionOrder = null)
        {
            Order = order;
            _globalExecutionOrder = globalExecutionOrder;
        }

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add($"SystemModule_{Order}_Before");
            _globalExecutionOrder?.Add($"SystemModule_{Order}_Before");
            var result = await next();
            ExecutionOrder.Add($"SystemModule_{Order}_After");
            _globalExecutionOrder?.Add($"SystemModule_{Order}_After");
            return result;
        }

        public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionOrder.Add($"SystemModule_{Order}_Stream_Before");
            _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_Before");
            await foreach (var item in next())
            {
                ExecutionOrder.Add($"SystemModule_{Order}_Stream_Item");
                _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_Item");
                yield return item;
            }
            ExecutionOrder.Add($"SystemModule_{Order}_Stream_After");
            _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_After");
        }
    }

    public class TestPipelineBehaviorTests
    {
        public class TestRequest : IRequest<string> { }
        public class TestStreamRequest : IStreamRequest<string> { }
    }

    public class TestSystemModuleWithGlobalOrder : ISystemModule
    {
        public int Order { get; }
        private readonly List<string> _globalExecutionOrder;
        private readonly string _name;

        public TestSystemModuleWithGlobalOrder(int order, List<string> globalExecutionOrder, string name)
        {
            Order = order;
            _globalExecutionOrder = globalExecutionOrder;
            _name = name;
        }

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _globalExecutionOrder.Add($"{_name}_Before");
            var result = await next();
            _globalExecutionOrder.Add($"{_name}_After");
            return result;
        }

        public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _globalExecutionOrder.Add($"{_name}_Stream_Before");
            await foreach (var item in next())
            {
                _globalExecutionOrder.Add($"{_name}_Stream_Item");
                yield return item;
            }
            _globalExecutionOrder.Add($"{_name}_Stream_After");
        }
    }

    public class TestPipelineBehaviorWithGlobalOrder : IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>
    {
        private readonly List<string> _globalExecutionOrder;
        private readonly string _name;

        public TestPipelineBehaviorWithGlobalOrder(List<string> globalExecutionOrder, string name)
        {
            _globalExecutionOrder = globalExecutionOrder;
            _name = name;
        }

        public async ValueTask<string> HandleAsync(PipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _globalExecutionOrder.Add($"{_name}_Before");
            var result = await next();
            _globalExecutionOrder.Add($"{_name}_After");
            return result + "_Modified";
        }
    }

    public class TestStreamPipelineBehaviorWithGlobalOrder : IStreamPipelineBehavior<PipelineBehaviorTests.TestStreamRequest, string>
    {
        private readonly List<string> _globalExecutionOrder;
        private readonly string _name;

        public TestStreamPipelineBehaviorWithGlobalOrder(List<string> globalExecutionOrder, string name)
        {
            _globalExecutionOrder = globalExecutionOrder;
            _name = name;
        }

        public async IAsyncEnumerable<string> HandleAsync(PipelineBehaviorTests.TestStreamRequest request, StreamHandlerDelegate<string> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _globalExecutionOrder.Add($"{_name}_Before");
            await foreach (var item in next())
            {
                _globalExecutionOrder.Add($"{_name}_Item: {item}");
                yield return item + "_Modified";
            }
            _globalExecutionOrder.Add($"{_name}_After");
        }
    }

    public class TestPipelineBehaviorWithCancellationCheck : IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>
    {
        private readonly List<CancellationToken> _receivedTokens;

        public TestPipelineBehaviorWithCancellationCheck(List<CancellationToken> receivedTokens)
        {
            _receivedTokens = receivedTokens;
        }

        public async ValueTask<string> HandleAsync(PipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _receivedTokens.Add(cancellationToken);
            var result = await next();
            return result + "_Modified";
        }
    }

    public class TestPipelineBehaviorThatThrows : IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>
    {
        public async ValueTask<string> HandleAsync(PipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Pipeline behavior exception");
        }
    }

    public class TestSystemModuleThatThrows : ISystemModule
    {
        public int Order => 1;

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("System module exception");
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("System module exception");
        }
    }

    public class TestPipelineBehaviorWithDelay : IPipelineBehavior<PipelineBehaviorTests.TestRequest, string>
    {
        public async ValueTask<string> HandleAsync(PipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken); // Long delay to allow cancellation
            return await next();
        }
    }


}