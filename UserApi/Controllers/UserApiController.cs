using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserDomain;

namespace UserApi.Controllers;

[Route("api/user")]
[ApiController]
public class UserApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GetUserProfile()
    {
        var name = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? User.FindFirst("email")?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new { name, email });
    }

    [HttpPatch("profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateProfile(UpdateUserDetailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            request.Email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                            ?? User.FindFirst("email")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;
        }

        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }
}
