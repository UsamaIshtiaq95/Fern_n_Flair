using Application.Request;
using Application.Response;
using Microsoft.AspNetCore.Identity;
using Moq;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Tests.Handlers;

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly Mock<IPasswordHasher<Users>> _hasherMock;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher<Users>>();
        _handler = new RegisterHandler(_repoMock.Object, _hasherMock.Object);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsErrorResponse()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("existing@test.com"))
                 .ReturnsAsync(1);

        var request = new RegisterRequest
        {
            Email = "existing@test.com",
            Name = "Test",
            Password = "Pass123!"
        };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("Email already registered", result.response);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Users>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewUser_RegistersSuccessfully()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("new@test.com"))
                 .ReturnsAsync(0);
        _hasherMock.Setup(h => h.HashPassword(It.IsAny<Users>(), "Pass123!"))
                   .Returns("hashed_password");

        var request = new RegisterRequest
        {
            Email = "new@test.com",
            Name = "New User",
            Password = "Pass123!"
        };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("User registered successfully", result.response);
        _repoMock.Verify(r => r.AddAsync(It.Is<Users>(u =>
            u.Email == "new@test.com" &&
            u.Name == "New User" &&
            u.Username == "new@test.com"
        )), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
