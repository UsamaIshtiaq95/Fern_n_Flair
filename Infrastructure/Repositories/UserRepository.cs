using Microsoft.EntityFrameworkCore;
using UserDomain;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetByEmailAsync(string email)
    {
        return await _context.Users.AsNoTracking().CountAsync(x => x.Email == email);
    }

    public async Task<Users> GetLoginAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            throw new NotFoundException("User not found");
        return user;
    }

    public async Task AddAsync(Users user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<int> UpdateDetailsAsync(Users user)
    {
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (dbUser == null)
            throw new NotFoundException("User not found");

        dbUser.Name = user.Name;
        dbUser.Email = user.Email;
        dbUser.UpdatedAt = DateTime.UtcNow;
        _context.Update(dbUser);

        return await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
