using Microsoft.EntityFrameworkCore;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Infrastructure.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly QuizVerseDbContext _context;

        public UserRepository(QuizVerseDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }

        public async Task<bool?> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }
    }

}