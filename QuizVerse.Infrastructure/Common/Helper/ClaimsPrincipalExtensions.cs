using System.Security.Claims;

namespace QuizVerse.Infrastructure.Common.Helper
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity is ClaimsIdentity identity)
            {
                var userIdClaim = identity.FindFirst(ClaimTypes.UserData);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    return userId;
            }
            return null;
        }

        public static string? GetUserEmail(this ClaimsPrincipal user)
        {
            return (user?.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
