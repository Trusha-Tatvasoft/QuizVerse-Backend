using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using QuizVerse.Infrastructure.Common.Exceptions;
using Microsoft.AspNetCore.Http;


namespace QuizVerse.Application.Core.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessTokenAsync(User? user)
        {
            if (user == null)
            {
                throw new ArgumentException("User Not Found.");
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshTokenAsync(User user, bool rememberMe)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim("remember_me", rememberMe.ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token must not be null or empty.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(
                _configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")
            );

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new AppException("Invalid token.");
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new AppException("Token has expired.", StatusCodes.Status401Unauthorized);
            }
            catch (SecurityTokenException)
            {
                throw new AppException("Invalid token.", StatusCodes.Status401Unauthorized);
            }
            catch (ArgumentException)
            {
                throw new AppException("Invalid token format.", StatusCodes.Status401Unauthorized);
            }
            catch (FormatException)
            {
                throw new AppException("Invalid token format.", StatusCodes.Status401Unauthorized);
            }
            catch (Exception)
            {
                throw new AppException("An error occurred while validating the token.", StatusCodes.Status500InternalServerError);
            }
        }

        public bool IsRememberMeEnabled(ClaimsPrincipal principal)
        {
            var rememberMeClaim = principal.FindFirst("remember_me")?.Value;
            return bool.TryParse(rememberMeClaim, out var rememberMe) && rememberMe;
        }

        public string GetUserIdFromToken(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.UserData)?.Value ?? string.Empty;
        }

        public DateTime GetTokenExpiration(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
    }
}