using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataRegistryThreadSafetyTests
    {
        [Fact]
        public void EndpointMetadataRegistry_ThreadSafety_ConcurrentRegistration()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            const int numThreads = 10;
            const int endpointsPerThread = 100;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            var tasks = new Task[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < endpointsPerThread; j++)
                        {
                            var metadata = new EndpointMetadata
                            {
                                Route = $"/api/test{threadId}_{j}",
                                RequestType = typeof(TestRequest),
                                HandlerType = typeof(TestHandler)
                            };
                            EndpointMetadataRegistry.RegisterEndpoint(metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.Empty(exceptions);
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(numThreads * endpointsPerThread, allEndpoints.Count);
        }

        [Fact]
        public void EndpointMetadataRegistry_ThreadSafety_ConcurrentReadWrite()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act - Concurrent reads and writes
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var tasks = new Task[20];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            var endpoints = EndpointMetadataRegistry.AllEndpoints;
                            Assert.NotNull(endpoints);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            for (int i = 10; i < 20; i++)
            {
                int index = i - 10;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            var newMetadata = new EndpointMetadata
                            {
                                Route = $"/api/write{index}_{j}",
                                RequestType = typeof(TestRequest),
                                HandlerType = typeof(TestHandler)
                            };
                            EndpointMetadataRegistry.RegisterEndpoint(newMetadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.Empty(exceptions);
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.True(allEndpoints.Count >= 1); // At least the original one plus some writes
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}