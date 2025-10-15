using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class EndpointMetadataTests
    {
        [Fact]
        public void EndpointMetadata_ShouldInitializeWithDefaults()
        {
            // Act
            var metadata = new EndpointMetadata();

            // Assert
            Assert.Empty(metadata.Route);
            Assert.Equal("POST", metadata.HttpMethod);
            Assert.Null(metadata.Version);
            Assert.Empty(metadata.HandlerMethodName);
            Assert.NotNull(metadata.Properties);
            Assert.Empty(metadata.Properties);
        }

        [Fact]
        public void EndpointMetadata_ShouldAllowSettingProperties()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "GET",
                Version = "v1",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleAsync"
            };

            // Assert
            Assert.Equal("/api/test", metadata.Route);
            Assert.Equal("GET", metadata.HttpMethod);
            Assert.Equal("v1", metadata.Version);
            Assert.Equal(typeof(TestRequest), metadata.RequestType);
            Assert.Equal(typeof(TestResponse), metadata.ResponseType);
            Assert.Equal(typeof(TestHandler), metadata.HandlerType);
            Assert.Equal("HandleAsync", metadata.HandlerMethodName);
        }

        [Fact]
        public void JsonSchemaContract_ShouldInitializeWithDefaults()
        {
            // Act
            var contract = new JsonSchemaContract();

            // Assert
            Assert.Empty(contract.Schema);
            Assert.Equal("application/json", contract.ContentType);
            Assert.Equal("http://json-schema.org/draft-07/schema#", contract.SchemaVersion);
            Assert.NotNull(contract.Properties);
            Assert.Empty(contract.Properties);
        }

        [Fact]
        public void JsonSchemaContract_ShouldAllowSettingProperties()
        {
            // Arrange
            var contract = new JsonSchemaContract
            {
                Schema = "{ \"type\": \"object\" }",
                ContentType = "application/xml",
                SchemaVersion = "v2.0"
            };

            // Assert
            Assert.Equal("{ \"type\": \"object\" }", contract.Schema);
            Assert.Equal("application/xml", contract.ContentType);
            Assert.Equal("v2.0", contract.SchemaVersion);
        }

        [Fact]
        public async Task EndpointMetadataRegistry_AsyncLocal_ScopeSharing()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            // Act - Register in main context
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            var mainContextEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Register in async context (Task.Run shares AsyncLocal with parent)
            await Task.Run(() =>
            {
                EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            });

            var afterAsyncEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert - AsyncLocal flows to Task.Run, so same scope is shared
            Assert.Single(mainContextEndpoints);
            Assert.Equal(metadata1, mainContextEndpoints.First());
            // After async operation, main context sees both endpoints (same scope)
            Assert.Equal(2, afterAsyncEndpoints.Count);
            Assert.Contains(metadata1, afterAsyncEndpoints);
            Assert.Contains(metadata2, afterAsyncEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_ShouldReturnMatchingEndpoints()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.Equal(2, endpoints.Count());
            Assert.Contains(metadata1, endpoints);
            Assert.Contains(metadata2, endpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_Generic_ShouldWork()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<TestRequest>();

            // Assert
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_Clear_ShouldRemoveAllEndpoints()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/test",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Act
            EndpointMetadataRegistry.Clear();

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Empty(allEndpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_AllEndpoints_ShouldReturnAllRegistered()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata { Route = "/test1", RequestType = typeof(TestRequest), HandlerType = typeof(TestHandler) };
            var metadata2 = new EndpointMetadata { Route = "/test2", RequestType = typeof(TestResponse), HandlerType = typeof(TestHandler) };
            var metadata3 = new EndpointMetadata { Route = "/test3", RequestType = typeof(TestRequest), HandlerType = typeof(TestHandler) };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Equal(3, allEndpoints.Count());
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
            Assert.Contains(metadata3, allEndpoints);
        }

        [Fact]
        public void EndpointMetadata_Properties_CanStoreCustomData()
        {
            // Arrange
            var metadata = new EndpointMetadata();

            // Act
            metadata.Properties["Custom1"] = "Value1";
            metadata.Properties["Custom2"] = 42;
            metadata.Properties["Custom3"] = true;

            // Assert
            Assert.Equal("Value1", metadata.Properties["Custom1"]);
            Assert.Equal(42, metadata.Properties["Custom2"]);
            Assert.Equal(true, metadata.Properties["Custom3"]);
        }

        [Fact]
        public void JsonSchemaContract_Properties_CanStoreCustomData()
        {
            // Arrange
            var contract = new JsonSchemaContract();

            // Act
            contract.Properties["required"] = new[] { "name", "age" };
            contract.Properties["additionalProperties"] = false;

            // Assert
            Assert.Equal(new[] { "name", "age" }, contract.Properties["required"]);
            Assert.Equal(false, contract.Properties["additionalProperties"]);
        }

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

        [Fact]
        public void EndpointMetadataRegistry_ScopeIsolation_BetweenDifferentScopes()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };

            // Act - Register in first scope
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            var endpointsInScope1 = EndpointMetadataRegistry.AllEndpoints;

            // Clear to create new scope
            EndpointMetadataRegistry.Clear();
            var endpointsAfterClear = EndpointMetadataRegistry.AllEndpoints;

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            var endpointsInScope2 = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Single(endpointsInScope1);
            Assert.Equal(metadata1, endpointsInScope1.First());
            Assert.Empty(endpointsAfterClear);
            Assert.Single(endpointsInScope2);
            Assert.Equal(metadata2, endpointsInScope2.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_ScopeManagement_EnsureScopeInitialized_NoLock()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();

            // Act - First call should initialize scope
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata1);

            // Register another in same scope
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(2, allEndpoints.Count);
            Assert.Contains(metadata1, allEndpoints);
            Assert.Contains(metadata2, allEndpoints);
        }


        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_ReturnsReadOnlyList()
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

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<EndpointMetadata>>(endpoints);
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_AllEndpoints_ReturnsReadOnlyList()
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

            // Act
            var endpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<EndpointMetadata>>(endpoints);
            Assert.Single(endpoints);
            Assert.Equal(metadata, endpoints.First());
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_WithNullType_ThrowsArgumentNullException()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EndpointMetadataRegistry.GetEndpointsForRequestType(null!));
        }

        [Fact]
        public void EndpointMetadataRegistry_GetEndpointsForRequestType_WithNonRegisteredType_ReturnsEmpty()
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

            // Act
            var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(string));

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void EndpointMetadataRegistry_MultipleEndpoints_SameRequestType_GroupedCorrectly()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata3 = new EndpointMetadata
            {
                Route = "/api/other",
                RequestType = typeof(TestResponse),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);
            EndpointMetadataRegistry.RegisterEndpoint(metadata3);

            // Act
            var testRequestEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));
            var testResponseEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestResponse));
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;

            // Assert
            Assert.Equal(2, testRequestEndpoints.Count);
            Assert.Contains(metadata1, testRequestEndpoints);
            Assert.Contains(metadata2, testRequestEndpoints);

            Assert.Single(testResponseEndpoints);
            Assert.Contains(metadata3, testResponseEndpoints);

            Assert.Equal(3, allEndpoints.Count);
        }

        [Fact]
        public void EndpointMetadataRegistry_LargeNumberOfEndpoints_Performance()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            const int numEndpoints = 10000;
            var endpoints = new List<EndpointMetadata>();

            for (int i = 0; i < numEndpoints; i++)
            {
                var metadata = new EndpointMetadata
                {
                    Route = $"/api/test{i}",
                    RequestType = typeof(TestRequest),
                    HandlerType = typeof(TestHandler)
                };
                endpoints.Add(metadata);
                EndpointMetadataRegistry.RegisterEndpoint(metadata);
            }

            // Act
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            var requestTypeEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest));

            // Assert
            Assert.Equal(numEndpoints, allEndpoints.Count);
            Assert.Equal(numEndpoints, requestTypeEndpoints.Count);
            foreach (var endpoint in endpoints)
            {
                Assert.Contains(endpoint, allEndpoints);
                Assert.Contains(endpoint, requestTypeEndpoints);
            }
        }

        [Fact]
        public void EndpointMetadataRegistry_Clear_ResetsScopeCompletely()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/test1",
                RequestType = typeof(TestRequest),
                HandlerType = typeof(TestHandler)
            };
            var metadata2 = new EndpointMetadata
            {
                Route = "/api/test2",
                RequestType = typeof(TestResponse),
                HandlerType = typeof(TestHandler)
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            // Act
            EndpointMetadataRegistry.Clear();

            // Assert
            Assert.Empty(EndpointMetadataRegistry.AllEndpoints);
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestRequest)));
            Assert.Empty(EndpointMetadataRegistry.GetEndpointsForRequestType(typeof(TestResponse)));
        }

        [Fact]
        public void EndpointMetadataRegistry_RegisterEndpoint_WithComplexMetadata()
        {
            // Arrange
            EndpointMetadataRegistry.Clear();
            var metadata = new EndpointMetadata
            {
                Route = "/api/complex/{id}",
                HttpMethod = "PUT",
                Version = "v2.1",
                RequestType = typeof(TestRequest),
                ResponseType = typeof(TestResponse),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleComplexAsync"
            };
            metadata.Properties["Authorization"] = "Bearer";
            metadata.Properties["CacheDuration"] = 300;

            // Act
            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Assert
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Single(allEndpoints);
            var registered = allEndpoints.First();
            Assert.Equal("/api/complex/{id}", registered.Route);
            Assert.Equal("PUT", registered.HttpMethod);
            Assert.Equal("v2.1", registered.Version);
            Assert.Equal(typeof(TestRequest), registered.RequestType);
            Assert.Equal(typeof(TestResponse), registered.ResponseType);
            Assert.Equal(typeof(TestHandler), registered.HandlerType);
            Assert.Equal("HandleComplexAsync", registered.HandlerMethodName);
            Assert.Equal("Bearer", registered.Properties["Authorization"]);
            Assert.Equal(300, registered.Properties["CacheDuration"]);
        }

        private class TestRequest : IRequest<TestResponse> { }
        private class TestResponse { }
        private class TestHandler { }
    }
}