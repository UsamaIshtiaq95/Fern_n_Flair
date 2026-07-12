using MediatR;
using Application.Response;
namespace Application.Request;
public class LoginRequest : IRequest<LoginResponse>
{
    public string Email { get; set; }
    public string Password { get; set; }
}
