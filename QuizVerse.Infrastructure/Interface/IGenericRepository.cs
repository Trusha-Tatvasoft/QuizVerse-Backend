using System.Linq.Expressions;

namespace QuizVerse.Infrastructure.Interface;

public interface IGenericRepository<T> where T : class
{
    Task<bool> Exists(Expression<Func<T, bool>> expression);

    Task<T?> GetAsync(Expression<Func<T, bool>> expression);

    Task<List<T>> GetAllAsync();

    Task<List<T>> FindAsync(Expression<Func<T, bool>> expression);

    Task AddAsync(T entity);

    Task AddRangeAsync(List<T> entities);

    Task UpdateAsync(T entity);

    Task DeleteAsync(T entity);

    Task DeleteRangeAsync(List<T> entity);

    void Detach(T entity);

    void ClearChangeTracker();

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}