using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using QuizVerse.Infrastructure.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common;


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
                throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration[SystemConstants.JWT_CONFIGURATION_KEY] ?? throw new InvalidOperationException("JWT Key is not configured.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration[SystemConstants.JWT_CONFIGURATION_ISSUER],
                audience: _configuration[SystemConstants.JWT_CONFIGURATION_AUDIENCE],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshTokenAsync(User user, bool rememberMe)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration[SystemConstants.JWT_CONFIGURATION_KEY] ?? throw new InvalidOperationException("JWT Key is not configured.")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(SystemConstants.REMEMBER_ME_CLAIM_NAME, rememberMe.ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _configuration[SystemConstants.JWT_CONFIGURATION_ISSUER],
                audience: _configuration[SystemConstants.JWT_CONFIGURATION_AUDIENCE],
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
                throw new ArgumentException(Constants.EMPTY_TOKEN_MESSAGE);
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(
                _configuration[SystemConstants.JWT_CONFIGURATION_KEY] ?? throw new InvalidOperationException(Constants.JWT_KEY_ERROR_MESSAGE)
            );

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration[SystemConstants.JWT_CONFIGURATION_ISSUER],
                ValidateAudience = true,
                ValidAudience = _configuration[SystemConstants.JWT_CONFIGURATION_AUDIENCE],
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new AppException(Constants.INVALID_TOKEN_MESSAGE);
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new AppException(Constants.EXPIRED_TOKEN_MESSAGE, StatusCodes.Status401Unauthorized);
            }
            catch (SecurityTokenException)
            {
                throw new AppException(Constants.INVALID_TOKEN_MESSAGE, StatusCodes.Status401Unauthorized);
            }
            catch (ArgumentException)
            {
                throw new AppException(Constants.INVALID_TOKEN_FORMAT_MESSAGE, StatusCodes.Status401Unauthorized);
            }
            catch (FormatException)
            {
                throw new AppException(Constants.INVALID_TOKEN_FORMAT_MESSAGE, StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex)
            {
                throw new AppException(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public bool IsRememberMeEnabled(ClaimsPrincipal principal)
        {
            var rememberMeClaim = principal.FindFirst(SystemConstants.REMEMBER_ME_CLAIM_NAME)?.Value;
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