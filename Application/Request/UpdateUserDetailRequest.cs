using Application.Response;
using MediatR;

namespace Application.Request;

public class UpdateUserDetailRequest : IRequest<UpdateUserDetailResponse>
{
    public string? Email { get; set; }

    public string Name { get; set; }
}