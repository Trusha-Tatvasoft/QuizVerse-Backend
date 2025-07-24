using System.Security.Claims;
using QuizVerse.Domain.Entities;

namespace QuizVerse.Application.Core.Interface
{
    public interface ITokenService
    {
        string GenerateAccessTokenAsync(User? user);

        string GenerateRefreshTokenAsync(User user, bool rememberMe);

        ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true);

        bool IsRememberMeEnabled(ClaimsPrincipal principal);

        string GetUserIdFromToken(ClaimsPrincipal principal);

        DateTime GetTokenExpiration(string token);
    }
}