using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
}
