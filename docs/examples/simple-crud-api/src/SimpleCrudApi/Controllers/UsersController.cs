using Microsoft.AspNetCore.Mvc;
using Relay.Core;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;

namespace SimpleCrudApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRelay _relay;

    public UsersController(IRelay relay)
    {
        _relay = relay;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(new GetUserQuery(id), cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var users = await _relay.SendAsync(new GetUsersQuery(page, pageSize), cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(
            new CreateUserCommand(request.Name, request.Email),
            cancellationToken);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(
            new UpdateUserCommand(id, request.Name, request.Email),
            cancellationToken);

        return user == null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await _relay.SendAsync(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}

// Request DTOs for API
public record CreateUserRequest(string Name, string Email);
public record UpdateUserRequest(string Name, string Email);