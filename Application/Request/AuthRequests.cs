using Application.Response;
using MediatR;

namespace Application.Request;

public class RefreshTokenRequest : IRequest<RefreshTokenResponse>
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}

public class RefreshTokenResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string Message { get; set; }
}

public class RevokeTokenRequest : IRequest<RevokeTokenResponse>
{
    public int UserId { get; set; }
}

public class RevokeTokenResponse
{
    public string Message { get; set; }
}
