namespace QuizVerse.Infrastructure.Interface;

public interface ISqlQueryRepository
{
    Task<TResult> SqlQuerySingleAsync<TResult>(string sql, params object[] parameters) where TResult : class;

    Task<List<TResult>> SqlQueryListAsync<TResult>(string sql, params object[] parameters) where TResult : class;
}
