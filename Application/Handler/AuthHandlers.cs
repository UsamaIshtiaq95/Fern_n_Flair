using System.IdentityModel.Tokens.Jwt;
using Application.Request;
using MediatR;
using Microsoft.AspNetCore.Identity;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Application.Handler;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<Users> _hasher;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        ITokenService tokenService,
        IPasswordHasher<Users> hasher)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _tokenService = tokenService;
        _hasher = hasher;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken);
        if (storedToken == null)
            throw new UnauthorizedException("Refresh token not found");

        if (storedToken.IsUsed)
            throw new UnauthorizedException("Refresh token has been used");

        if (storedToken.IsRevoked)
            throw new UnauthorizedException("Refresh token has been revoked");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired");

        var jti = new JwtSecurityTokenHandler().ReadJwtToken(request.Token).Id;
        if (storedToken.JwtId != jti)
            throw new UnauthorizedException("Token does not match");

        await _refreshTokenRepo.MarkAsUsedAsync(storedToken.TokenId);

        var user = storedToken.User;
        var newToken = _tokenService.CreateToken(user, out var newJti);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            JwtId = newJti,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays())
        };

        await _refreshTokenRepo.CreateAsync(refreshTokenEntity);
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Message = "Token refreshed successfully"
        };
    }
}

public class RevokeTokenHandler : IRequestHandler<RevokeTokenRequest, RevokeTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public RevokeTokenHandler(IRefreshTokenRepository refreshTokenRepo)
    {
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<RevokeTokenResponse> Handle(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _refreshTokenRepo.RevokeAllUserTokensAsync(request.UserId);
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new RevokeTokenResponse { Message = "Tokens revoked successfully" };
    }
}
