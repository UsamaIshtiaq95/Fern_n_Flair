using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserDomain;

namespace UserAuthApi.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new BadRequestException("Null data inserted");

        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new BadRequestException("Null data inserted");

        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
            return Unauthorized();

        var result = await _mediator.Send(new RevokeTokenRequest { UserId = userId }, cancellationToken);
        return Ok(result);
    }
}
