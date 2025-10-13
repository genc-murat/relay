using Microsoft.AspNetCore.Mvc;
using Relay.Core.Contracts.Core;
using Relay.ControllerApiSample.Features.Users;

namespace Relay.ControllerApiSample.Controllers;

/// <summary>
/// Users controller demonstrating Relay framework usage with ASP.NET Core controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IRelay _relay;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IRelay relay, ILogger<UsersController> logger)
    {
        _relay = relay;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllUsersResponse>> GetAllUsers(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET api/users - Retrieving all users");

        var request = new GetAllUsersRequest();
        var response = await _relay.SendAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details if found, NotFound otherwise</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetUserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET api/users/{Id} - Retrieving user", id);

        var request = new GetUserRequest(id);
        var response = await _relay.SendAsync(request, cancellationToken);

        if (response == null)
        {
            _logger.LogWarning("User with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST api/users - Creating new user: {Name}", request.Name);

        var response = await _relay.SendAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = response.Id },
            response);
    }
}
