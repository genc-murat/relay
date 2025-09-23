using Microsoft.AspNetCore.Mvc;
using Relay.Core;
using MinimalApi.Models;

namespace MinimalApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        // GET /api/users/{id}
        group.MapGet("/{id:int}", async (int id, IRelay relay, CancellationToken cancellationToken) =>
        {
            var user = await relay.SendAsync(new GetUserQuery(id), cancellationToken);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetUser")
        .WithSummary("Get user by ID")
        .Produces<User>()
        .Produces(404);

        // GET /api/users
        group.MapGet("/", async ([FromQuery] int page, [FromQuery] int pageSize, IRelay relay, CancellationToken cancellationToken) =>
        {
            // Default values
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var users = await relay.SendAsync(new GetUsersQuery(page, pageSize), cancellationToken);
            return Results.Ok(users);
        })
        .WithName("GetUsers")
        .WithSummary("Get paginated list of users")
        .Produces<IEnumerable<User>>();

        // POST /api/users
        group.MapPost("/", async (CreateUserRequest request, IRelay relay, CancellationToken cancellationToken) =>
        {
            var user = await relay.SendAsync(
                new CreateUserCommand(request.Name, request.Email),
                cancellationToken);

            return Results.CreatedAtRoute("GetUser", new { id = user.Id }, user);
        })
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .Accepts<CreateUserRequest>("application/json")
        .Produces<User>(201)
        .ProducesValidationProblem();

        // PUT /api/users/{id}
        group.MapPut("/{id:int}", async (int id, UpdateUserRequest request, IRelay relay, CancellationToken cancellationToken) =>
        {
            var user = await relay.SendAsync(
                new UpdateUserCommand(id, request.Name, request.Email),
                cancellationToken);

            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("UpdateUser")
        .WithSummary("Update an existing user")
        .Accepts<UpdateUserRequest>("application/json")
        .Produces<User>()
        .Produces(404)
        .ProducesValidationProblem();

        // DELETE /api/users/{id}
        group.MapDelete("/{id:int}", async (int id, IRelay relay, CancellationToken cancellationToken) =>
        {
            await relay.SendAsync(new DeleteUserCommand(id), cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithSummary("Delete a user")
        .Produces(204);
    }
}