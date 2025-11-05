using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    public class PipelineIntegrationTests
    {
        [Fact]
        public async Task PipelineExecutor_Should_Execute_Pipeline_Before_Handler()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);
            var executionOrder = new List<string>();

            var request = new IntegrationTestRequest { Value = "test" };

            ValueTask<string> Handler(IntegrationTestRequest req, CancellationToken ct)
            {
                executionOrder.Add("Handler");
                return new ValueTask<string>($"Handled: {req.Value}");
            }

            // Act
            var result = await executor.ExecuteAsync<IntegrationTestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.Equal("Handled: test", result);
            Assert.Single(executionOrder);
            Assert.Equal("Handler", executionOrder[0]);
        }

        [Fact]
        public void PipelineExecutor_Should_Execute_Stream_Handler()
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
                yield return "Item 1";
                yield return "Item 2";
                yield return "Item 3";
            }

            // Act
            var result = executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TestPipelineBehavior_Should_Modify_Request_And_Response()
        {
            // Arrange
            var behavior = new TestPipelineBehavior();
            var request = new TestRequest();

            ValueTask<string> next() => new ValueTask<string>("response");

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("response_Modified", result);
            Assert.Equal(2, behavior.ExecutionOrder.Count);
            Assert.Equal("Before", behavior.ExecutionOrder[0]);
            Assert.Equal("After", behavior.ExecutionOrder[1]);
        }

        [Fact]
        public async Task TestStreamPipelineBehavior_Should_Modify_Stream_Items()
        {
            // Arrange
            var behavior = new TestStreamPipelineBehavior();
            var request = new TestStreamRequest();

            IAsyncEnumerable<string> next()
            {
                return GenerateTestItems();
            }

            static async IAsyncEnumerable<string> GenerateTestItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var results = new List<string>();
            await foreach (var item in behavior.HandleAsync(request, next, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal("item1_Modified", results[0]);
            Assert.Equal("item2_Modified", results[1]);
            Assert.Contains("Before", behavior.ExecutionOrder);
            Assert.Contains("After", behavior.ExecutionOrder);
        }

        [Fact]
        public void TestSystemModule_Should_Have_Correct_Order()
        {
            // Arrange
            var module1 = new TestSystemModule(1);
            var module2 = new TestSystemModule(2);

            // Act & Assert
            Assert.Equal(1, module1.Order);
            Assert.Equal(2, module2.Order);
        }

        [Fact]
        public async Task TestSystemModule_Should_Execute_Around_Handler()
        {
            // Arrange
            var module = new TestSystemModule(0);

            ValueTask<string> next() => new ValueTask<string>("result");

            // Act
            var result = await module.ExecuteAsync<object, string>(new object(), next, CancellationToken.None);

            // Assert
            Assert.Equal("result", result);
            Assert.Equal(2, module.ExecutionOrder.Count);
            Assert.Equal("SystemModule_0_Before", module.ExecutionOrder[0]);
            Assert.Equal("SystemModule_0_After", module.ExecutionOrder[1]);
        }

        // Test request types
        public class IntegrationTestRequest : IRequest<string>
        {
            public string Value { get; set; } = string.Empty;
        }


    }
}
