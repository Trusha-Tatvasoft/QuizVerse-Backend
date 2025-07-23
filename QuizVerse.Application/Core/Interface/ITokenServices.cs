using QuizVerse.Domain.Entities;

namespace QuizVerse.Application.Core.Interface
{
    public interface ITokenService
    {
        string GenerateAccessTokenAsync(User? user);

        string GenerateRefreshTokenAsync();  
    }
}