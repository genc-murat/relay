using System;
using System.Linq;
using Relay.Core;

// Simple test to verify endpoint metadata functionality
class Program
{
    static void Main()
    {
        Console.WriteLine("Testing Endpoint Metadata functionality...");

        // Clear registry
        EndpointMetadataRegistry.Clear();

        // Create test metadata
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            Version = "v1",
            RequestType = typeof(string),
            ResponseType = typeof(int),
            HandlerType = typeof(Program),
            HandlerMethodName = "TestHandler",
            RequestSchema = new JsonSchemaContract
            {
                Schema = @"{ ""type"": ""string"" }",
                ContentType = "application/json"
            },
            ResponseSchema = new JsonSchemaContract
            {
                Schema = @"{ ""type"": ""integer"" }",
                ContentType = "application/json"
            }
        };

        // Register endpoint
        EndpointMetadataRegistry.RegisterEndpoint(metadata);

        // Verify registration
        var allEndpoints = EndpointMetadataRegistry.AllEndpoints;
        Console.WriteLine($"Total endpoints registered: {allEndpoints.Count}");

        if (allEndpoints.Count == 1)
        {
            var endpoint = allEndpoints.First();
            Console.WriteLine($"Route: {endpoint.Route}");
            Console.WriteLine($"HTTP Method: {endpoint.HttpMethod}");
            Console.WriteLine($"Version: {endpoint.Version}");
            Console.WriteLine($"Request Type: {endpoint.RequestType.Name}");
            Console.WriteLine($"Response Type: {endpoint.ResponseType?.Name}");
            Console.WriteLine($"Handler Type: {endpoint.HandlerType.Name}");
            Console.WriteLine($"Handler Method: {endpoint.HandlerMethodName}");
            Console.WriteLine($"Request Schema: {endpoint.RequestSchema?.Schema}");
            Console.WriteLine($"Response Schema: {endpoint.ResponseSchema?.Schema}");
        }

        // Test retrieval by request type
        var endpointsForString = EndpointMetadataRegistry.GetEndpointsForRequestType<string>();
        Console.WriteLine($"Endpoints for string type: {endpointsForString.Count}");

        var endpointsForInt = EndpointMetadataRegistry.GetEndpointsForRequestType<int>();
        Console.WriteLine($"Endpoints for int type: {endpointsForInt.Count}");

        Console.WriteLine("Endpoint Metadata test completed successfully!");
    }
}