using Domain.Models;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfTagRepository : ITagRepository
{
    private readonly AppDbContext _context;

    public EfTagRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tag>> GetTagsAsync()
    {
        return await _context.Tags
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Tag?> GetTagByIdAsync(Guid id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task CreateTagAsync(Tag tag)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTagAsync(Guid id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag is not null)
        {
            _context.Tags.Remove(tag);
        }

        await _context.SaveChangesAsync();
    }
}
