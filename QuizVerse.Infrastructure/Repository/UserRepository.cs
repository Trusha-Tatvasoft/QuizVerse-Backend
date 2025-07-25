using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Infrastructure.Repository;

public class UserRepository(QuizVerseDbContext _context) : GenericRepository<User>(_context), IUserRepository
{
}
