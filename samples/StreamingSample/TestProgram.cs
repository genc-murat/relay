using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddRelayConfiguration();
    services.AddTransient<TestService>();
});

var host = builder.Build();
var relay = host.Services.GetRequiredService<IRelay>();

Console.WriteLine("🚀 Testing Relay Sample");
var result = await relay.SendAsync(new TestRequest("Hello Relay!"));
Console.WriteLine($"✅ Result: {result.Message}");

public record TestRequest(string Input) : IRequest<TestResponse>;
public record TestResponse(string Message);

public class TestService
{
    [Handle]
    public ValueTask<TestResponse> Handle(TestRequest request, CancellationToken ct)
    {
        return ValueTask.FromResult(new TestResponse($"Processed: {request.Input}"));
    }
}
