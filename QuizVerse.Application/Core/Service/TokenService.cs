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
    public class TokenService(IConfiguration _configuration) : ITokenService
    {
        public string GenerateAccessToken(User? user)
        {
            if (user == null)
            {
                throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var expiryMinutesString = _configuration["AccessTokenExpiryMinutes"] ?? throw new InvalidOperationException(Constants.ACCESS_TOKEN_EXPIRYTIME_NOT_CONFIGURED_MESSAGE);
            return CreateToken(claims, DateTime.UtcNow.AddMinutes(double.Parse(expiryMinutesString)));
        }

        public string GenerateRefreshToken(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(SystemConstants.REMEMBER_ME_CLAIM_NAME, rememberMe.ToString())
            };
            var expiryDaysString = _configuration["RefreshTokenExpiryDays"] ?? throw new InvalidOperationException(Constants.REFRESH_TOKEN_EXPIRYTIME_NOT_CONFIGURED_MESSAGE);
            return CreateToken(claims, DateTime.UtcNow.AddDays(double.Parse(expiryDaysString)));
        }

        private string CreateToken(IEnumerable<Claim> claims, DateTime expires)
        {
            var keyString = _configuration["JwtSettings:Key"] ?? throw new InvalidOperationException(Constants.JWT_KEY_ERROR_MESSAGE);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: expires,
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
                _configuration["JwtSettings:Key"] ?? throw new InvalidOperationException(Constants.JWT_KEY_ERROR_MESSAGE)
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
                    throw new AppException(Constants.INVALID_TOKEN_FORMAT_MESSAGE);
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new AppException(Constants.EXPIRED_TOKEN_MESSAGE, StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex) when (
                ex is SecurityTokenException ||
                ex is ArgumentException ||
                ex is FormatException
            )
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

        public string GenerateResetPasswordToken(User? user)
        {
            if (user == null)
            {
                throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
            };
            var expiryMinutesString = _configuration["ResetPasswordTokenExpiryMinutes"] ?? throw new InvalidOperationException(Constants.ACCESS_TOKEN_EXPIRYTIME_NOT_CONFIGURED_MESSAGE);
            return CreateToken(claims, DateTime.UtcNow.AddMinutes(double.Parse(expiryMinutesString)));
        }
    }
}