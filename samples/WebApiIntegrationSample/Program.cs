using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Relay;
using Relay.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRelayConfiguration();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Always enable Swagger for demo
app.UseSwagger();
app.UseSwaggerUI();

// Product endpoints
app.MapGet("/api/products", async (IRelay relay) =>
{
    var query = new GetAllProductsQuery();
    var products = await relay.SendAsync(query);
    return Results.Ok(products);
})
.WithName("GetAllProducts")
.WithOpenApi();

app.MapGet("/api/products/{id:int}", async (int id, IRelay relay) =>
{
    var query = new GetProductByIdQuery(id);
    var product = await relay.SendAsync(query);
    return product != null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithOpenApi();

app.MapPost("/api/products", async ([FromBody] CreateProductRequest request, IRelay relay) =>
{
    var command = new CreateProductCommand(request.Name, request.Price, request.Stock);
    var product = await relay.SendAsync(command);
    return Results.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

app.MapDelete("/api/products/{id:int}", async (int id, IRelay relay) =>
{
    var command = new DeleteProductCommand(id);
    var success = await relay.SendAsync(command);
    return success ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteProduct")
.WithOpenApi();

Console.WriteLine("🚀 Relay Web API Integration Sample");
Console.WriteLine("Running on: http://localhost:5000");
Console.WriteLine("Swagger UI: http://localhost:5000/swagger");

app.Run("http://localhost:5000");

// Models
public record Product(int Id, string Name, decimal Price, int Stock);

// Requests
public record CreateProductRequest([Required] string Name, [Range(0.01, double.MaxValue)] decimal Price, [Range(0, int.MaxValue)] int Stock);

// Queries and Commands
public record GetAllProductsQuery() : IRequest<List<Product>>;
public record GetProductByIdQuery(int Id) : IRequest<Product?>;
public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<Product>;
public record DeleteProductCommand(int Id) : IRequest<bool>;

// Services
public class ProductService
{
    private static List<Product> _products = new()
    {
        new Product(1, "Laptop", 999.99m, 10),
        new Product(2, "Mouse", 29.99m, 50),
        new Product(3, "Keyboard", 79.99m, 30)
    };
    private static int _nextId = 4;

    [Handle]
    public ValueTask<List<Product>> GetAllProducts(GetAllProductsQuery query, CancellationToken ct)
    {
        return ValueTask.FromResult(_products.ToList());
    }

    [Handle]
    public ValueTask<Product?> GetProductById(GetProductByIdQuery query, CancellationToken ct)
    {
        var product = _products.FirstOrDefault(p => p.Id == query.Id);
        return ValueTask.FromResult(product);
    }

    [Handle]
    public ValueTask<Product> CreateProduct(CreateProductCommand command, CancellationToken ct)
    {
        var product = new Product(_nextId++, command.Name, command.Price, command.Stock);
        _products.Add(product);
        return ValueTask.FromResult(product);
    }

    [Handle]
    public ValueTask<bool> DeleteProduct(DeleteProductCommand command, CancellationToken ct)
    {
        var product = _products.FirstOrDefault(p => p.Id == command.Id);
        if (product == null) return ValueTask.FromResult(false);

        _products.Remove(product);
        return ValueTask.FromResult(true);
    }
}

public class OrderService
{
    // Order service placeholder
}
