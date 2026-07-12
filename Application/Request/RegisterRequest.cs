using Application.Response;
using MediatR;

namespace Application.Request;

public class RegisterRequest : IRequest<UpdateUserDetailResponse>
{
    public string Email { get; set; }

    public string Name { get; set; }
    public string Password { get; set; }
}