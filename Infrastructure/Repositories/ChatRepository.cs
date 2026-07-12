using Microsoft.EntityFrameworkCore;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Infrastructure.Repositories;

public class ChatRepository : Repository<Chats>, IChatRepository
{
    public ChatRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Chats>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Room)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Chats>> GetByRoomIdAsync(int roomId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Room)
            .Where(c => c.RoomId == roomId)
            .ToListAsync();
    }
}
