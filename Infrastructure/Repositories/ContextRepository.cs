using Microsoft.EntityFrameworkCore;
using UserDomain.Entities;
using UserDomain.Interface;

namespace Infrastructure.Repositories;

public class ContextRepository : Repository<Contexts>, IContextRepository
{
    public ContextRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Contexts?> GetByTypeAsync(string type)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Type == type);
    }
}
