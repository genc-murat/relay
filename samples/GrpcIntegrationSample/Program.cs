using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Relay;
using Relay.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddRelayConfiguration();
builder.Services.AddScoped<UserServiceImplementation>();

var app = builder.Build();

app.MapGet("/", () => "gRPC server running. ⚠️  Note: This is a demo - full gRPC requires .proto files");

Console.WriteLine("🚀 Relay gRPC Integration Demo");
Console.WriteLine("Server running on: http://localhost:5001");
Console.WriteLine();
Console.WriteLine("⚠️  Note: Full gRPC integration requires:");
Console.WriteLine("  • .proto file definitions");
Console.WriteLine("  • gRPC client/server configuration");
Console.WriteLine("  • Service registration");
Console.WriteLine();

app.Run("http://localhost:5001");

// Relay commands and queries
public record CreateUserCommand(string Name, string Email) : IRequest<UserDto>;
public record GetUserQuery(int Id) : IRequest<UserDto?>;
public record UserDto(int Id, string Name, string Email);

// Relay handler
public class UserServiceImplementation
{
    private static List<UserDto> _users = new()
    {
        new UserDto(1, "John Doe", "john@example.com"),
        new UserDto(2, "Jane Smith", "jane@example.com")
    };
    private static int _nextId = 3;

    [Handle]
    public ValueTask<UserDto> CreateUser(CreateUserCommand command, CancellationToken ct)
    {
        var user = new UserDto(_nextId++, command.Name, command.Email);
        _users.Add(user);
        Console.WriteLine($"✅ Created user: {user.Name}");
        return ValueTask.FromResult(user);
    }

    [Handle]
    public ValueTask<UserDto?> GetUser(GetUserQuery query, CancellationToken ct)
    {
        var user = _users.FirstOrDefault(u => u.Id == query.Id);
        Console.WriteLine($"📖 Retrieved user: {user?.Name ?? "Not found"}");
        return ValueTask.FromResult(user);
    }
}
