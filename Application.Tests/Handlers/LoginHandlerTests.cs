using Application.Request;
using Application.Response;
using Microsoft.AspNetCore.Identity;
using Moq;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Tests.Handlers;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _repoMock;
    private readonly Mock<IPasswordHasher<Users>> _hasherMock;
    private readonly Mock<ITokenService> _tokenMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _repoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher<Users>>();
        _tokenMock = new Mock<ITokenService>();
        _handler = new LoginHandler(_repoMock.Object, _hasherMock.Object, _tokenMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsInvalidCredentials()
    {
        _repoMock.Setup(r => r.GetLoginAsync("unknown@test.com"))
                 .ReturnsAsync((Users?)null);

        var request = new LoginRequest { Email = "unknown@test.com", Password = "pass" };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("", result.Token);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = new Users
        {
            Email = "user@test.com",
            PasswordHash = "correct_hash"
        };

        _repoMock.Setup(r => r.GetLoginAsync("user@test.com"))
                 .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyHashedPassword(user, "correct_hash", "wrong_pass"))
                   .Returns(PasswordVerificationResult.Failed);

        var request = new LoginRequest { Email = "user@test.com", Password = "wrong_pass" };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("Invalid credentials", result.Message);
        _tokenMock.Verify(t => t.CreateToken(It.IsAny<Users>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = new Users
        {
            Id = 1,
            Email = "user@test.com",
            Name = "Test User",
            PasswordHash = "correct_hash"
        };

        _repoMock.Setup(r => r.GetLoginAsync("user@test.com"))
                 .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyHashedPassword(user, "correct_hash", "correct_pass"))
                   .Returns(PasswordVerificationResult.Success);
        _tokenMock.Setup(t => t.CreateToken(user))
                  .Returns("jwt_token_here");

        var request = new LoginRequest { Email = "user@test.com", Password = "correct_pass" };

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Equal("jwt_token_here", result.Token);
        Assert.Equal("Login successful", result.Message);
    }
}
