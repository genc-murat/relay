using Relay.Core.Metadata.Endpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Metadata;

public class EndpointMetadataRegistryEnsureScopeInitializedTests
{
    public EndpointMetadataRegistryEnsureScopeInitializedTests()
    {
        // Ensure clean state for each test
        EndpointMetadataRegistry.Clear();
    }

    [Fact]
    public async Task EnsureScopeInitialized_NoLock_ConcurrentAccess_ThreadSafety()
    {
        // Arrange - Clear the registry
        EndpointMetadataRegistry.Clear();
        
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - Start multiple concurrent tasks that will all try to register endpoints
        for (int i = 0; i < 20; i++)
        {
            var taskId = i;
            var task = Task.Run(() =>
            {
                try
                {
                    var metadata = new EndpointMetadata
                    {
                        RequestType = typeof(string),
                        ResponseType = typeof(int),
                        HttpMethod = "GET",
                        Route = $"/test{taskId}"
                    };
                    EndpointMetadataRegistry.RegisterEndpoint(metadata);
                    
                    // Also test getting endpoints
                    var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();
                    var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert - No exceptions should have occurred
        Assert.Empty(exceptions);
        
        // All endpoints should be registered
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Assert.NotEmpty(allEndpoints);
        Assert.Equal(20, allEndpoints.Count);
    }

    [Fact]
    public async Task EnsureScopeInitialized_NoLock_MultipleScopes_ConcurrentAccess()
    {
        // Arrange - Clear the registry
        EndpointMetadataRegistry.Clear();
        
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Act - Start multiple concurrent tasks that will work with different scopes
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            var task = Task.Run(() =>
            {
                try
                {
                    // Each task registers a few endpoints
                    for (int j = 0; j < 3; j++)
                    {
                        var metadata = new EndpointMetadata
                        {
                            RequestType = typeof(string),
                            ResponseType = typeof(int),
                            HttpMethod = "GET",
                            Route = $"/test{taskId}-{j}"
                        };
                        EndpointMetadataRegistry.RegisterEndpoint(metadata);
                    }
                    
                    // Access the endpoints
                    var endpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();
                    var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert - No exceptions should have occurred
        Assert.Empty(exceptions);
        
        // Check that all endpoints are registered
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Assert.Equal(30, allEndpoints.Count); // 10 tasks × 3 endpoints each
    }

    [Fact]
    public void EnsureScopeInitialized_NoLock_HeavyLoad_StressTest()
    {
        // Arrange - Clear the registry
        EndpointMetadataRegistry.Clear();

        // Act - Register many endpoints rapidly
        const int endpointCount = 1000;
        for (int i = 0; i < endpointCount; i++)
        {
            var metadata = new EndpointMetadata
            {
                RequestType = typeof(string),
                ResponseType = typeof(int),
                HttpMethod = "GET",
                Route = $"/test{i}"
            };
            EndpointMetadataRegistry.RegisterEndpoint(metadata);
        }

        // Assert - All endpoints should be registered
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Assert.Equal(endpointCount, allEndpoints.Count);
        
        // Test retrieval performance
        var requestTypeEndpoints = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();
        Assert.Equal(endpointCount, requestTypeEndpoints.Count);
    }

    [Fact]
    public async Task EnsureScopeInitialized_NoLock_ClearAndReinitialize_MultipleTimes()
    {
        // Act - Clear and reinitialize multiple times
        for (int cycle = 0; cycle < 50; cycle++)
        {
            // Register some endpoints
            for (int i = 0; i < 5; i++)
            {
                var metadata = new EndpointMetadata
                {
                    RequestType = typeof(string),
                    ResponseType = typeof(int),
                    HttpMethod = "GET",
                    Route = $"/cycle{cycle}-endpoint{i}"
                };
                EndpointMetadataRegistry.RegisterEndpoint(metadata);
            }
            
            // Verify endpoints are registered
            var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Equal(5, allEndpoints.Count);
            
            // Clear the registry
            EndpointMetadataRegistry.Clear();
            
            // Verify it's cleared
            var clearedEndpoints = EndpointMetadataRegistry.AllEndpoints;
            Assert.Empty(clearedEndpoints);
        }
    }

    [Fact]
    public async Task EnsureScopeInitialized_NoLock_ParallelRegistrations_AndQueries()
    {
        // Arrange
        EndpointMetadataRegistry.Clear();
        
        var registrationTasks = new List<Task>();
        var queryTasks = new List<Task<List<EndpointMetadata>>>();

        // Act - Run registrations and queries in parallel
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            
            // Registration task
            var registrationTask = Task.Run(() =>
            {
                var metadata = new EndpointMetadata
                {
                    RequestType = typeof(string),
                    ResponseType = typeof(int),
                    HttpMethod = "GET",
                    Route = $"/parallel-test{index}"
                };
                EndpointMetadataRegistry.RegisterEndpoint(metadata);
            });
            registrationTasks.Add(registrationTask);
            
            // Query task (every 5th iteration)
            if (i % 5 == 0)
            {
                var queryTask = Task.Run(() =>
                {
                    return EndpointMetadataRegistry.GetEndpointsForRequestType<string>().ToList();
                });
                queryTasks.Add(queryTask);
            }
        }

        // Wait for all registration tasks
        await Task.WhenAll(registrationTasks);
        
        // Wait for all query tasks
        var queryResults = await Task.WhenAll(queryTasks);

        // Assert - All endpoints should be registered
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Assert.Equal(50, allEndpoints.Count);
        
        // Query results should be consistent (though may vary in count as registrations happen)
        Assert.NotEmpty(queryResults);
    }
}