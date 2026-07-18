using Microsoft.EntityFrameworkCore;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        return token;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task MarkAsUsedAsync(int tokenId)
    {
        var token = await _context.RefreshTokens.FindAsync(tokenId);
        if (token != null)
        {
            token.IsUsed = true;
        }
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && !t.IsUsed)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
