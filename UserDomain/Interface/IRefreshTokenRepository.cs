using UserDomain.Entities;

namespace UserDomain.Interface;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task MarkAsUsedAsync(int tokenId);
    Task RevokeAllUserTokensAsync(int userId);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
