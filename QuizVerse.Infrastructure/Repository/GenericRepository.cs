using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Domain.Data;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Infrastructure.Repository;

public class GenericRepository<T>(QuizVerseDbContext _context) : IGenericRepository<T>
           where T : class
{
    public async Task<bool> Exists(Expression<Func<T, bool>> expression)
    {
        return await _context.Set<T>().AsNoTracking().Where(expression).AnyAsync();
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        var query = _context.Set<T>().AsNoTracking().Where(expression);

        if (includes != null)
        {
            query = includes(query);
        }

        return await query.FirstOrDefaultAsync().ConfigureAwait(false);
    }
    public async Task<List<T>> GetAllAsync()
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync();
    }

    public IQueryable<T> GetQueryableInclude(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _context.Set<T>().AsNoTracking();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query;
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> expression)
    {
        return await _context.Set<T>().AsNoTracking().Where(expression).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(List<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(List<T> entity)
    {
        _context.Set<T>().RemoveRange(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        DbSet<T> dbSet = _context.Set<T>();

        if (predicate == null)
        {
            return await dbSet.CountAsync();
        }
        else
        {
            return await dbSet.CountAsync(predicate);
        }
    }
}