using QuizVerse.Domain.Entities;

namespace QuizVerse.Infrastructure.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool?> UpdateUserAsync(User user);
    }
}