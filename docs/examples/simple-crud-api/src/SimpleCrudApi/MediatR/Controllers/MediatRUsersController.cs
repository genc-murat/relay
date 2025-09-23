using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;
using SimpleCrudApi.MediatR.Requests;

namespace SimpleCrudApi.MediatR.Controllers;

[ApiController]
[Route("api/mediatr/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new MediatRGetUserQuery(id), cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var users = await _mediator.Send(new MediatRGetUsersQuery(page, pageSize), cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new MediatRCreateUserCommand(request.Name, request.Email),
            cancellationToken);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new MediatRUpdateUserCommand(id, request.Name, request.Email),
            cancellationToken);

        return user == null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MediatRDeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}