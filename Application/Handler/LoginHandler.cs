using Application.Request;
using Application.Response;
using MediatR;
using Microsoft.AspNetCore.Identity;
using UserDomain.Entities;
using UserDomain.Interface;

public class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher<Users> _hasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public LoginHandler(IUserRepository repo, IPasswordHasher<Users> hasher, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepo)
    {
        _repo = repo;
        _hasher = hasher;
        _tokenService = tokenService;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _repo.GetLoginAsync(request.Email);
        if (user == null)
            return new LoginResponse { Token = "", RefreshToken = "", Message = "Invalid credentials" };

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result != PasswordVerificationResult.Success)
            return new LoginResponse { Token = "", RefreshToken = "", Message = "Invalid credentials" };

        string token = _tokenService.CreateToken(user, out var jwtId);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokenRepo.CreateAsync(new RefreshToken
        {
            Token = refreshToken,
            JwtId = jwtId,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays())
        });
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new LoginResponse { Token = token, RefreshToken = refreshToken, Message = "Login successful" };
    }
}
