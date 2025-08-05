using Microsoft.EntityFrameworkCore;
using QuizVerse.Domain.Data;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Infrastructure.Repository;

public class SqlQueryRepository(QuizVerseDbContext _context) : ISqlQueryRepository
{
    public async Task<TResult> SqlQuerySingleAsync<TResult>(string sql, params object[] parameters) where TResult : class
    {
        return await _context.Database
                             .SqlQueryRaw<TResult>(sql, parameters)
                             .AsNoTracking()
                             .SingleAsync();
    }

    public async Task<List<TResult>> SqlQueryListAsync<TResult>(string sql, params object[] parameters) where TResult : class
    {
        return await _context.Database
                             .SqlQueryRaw<TResult>(sql, parameters)
                             .AsNoTracking()
                             .ToListAsync();
    }
}
