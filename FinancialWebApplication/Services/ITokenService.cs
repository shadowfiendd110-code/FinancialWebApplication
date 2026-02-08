using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApplication3.Models;


namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс для работы с токенами.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Генерирует JWT-токен.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>JWT-токен и его время жизни.</returns>
        (string Token, DateTime ExpiresAt) GenerateToken(User user);

        /// <summary>
        /// Генерирует рефреш токен.
        /// </summary>
        /// <returns>Рефреш токен.</returns>
        RefreshToken GenerateRefreshToken();
    }

    /// <summary>
    /// Сервис для работы с токенами.
    /// </summary>
    public class TokenService: ITokenService
    {
        /// <summary>
        /// Помощник работы с конфигурацией.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Инициализация сервиса.
        /// </summary>
        /// <param name="configuration">Помощник работы с конфигурацией.</param>
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Генерирует рефреш токен.
        /// </summary>
        /// <returns>Рефреш токен.</returns>
        public RefreshToken GenerateRefreshToken()
        {
            var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);

            var refreshToken = Convert.ToBase64String(refreshTokenBytes);

            var expires = DateTime.UtcNow.AddDays(30);

            return new RefreshToken
            {
                Token = refreshToken,
                Expires = expires,
                Created = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// Генерирует JWT-токен.
        /// </summary>
        /// <param name="user">Пользователь.</param>
        /// <returns>JWT-токен.</returns>
        public (string Token, DateTime ExpiresAt) GenerateToken(User user)
        {
            //Получаем настройки
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var secret = _configuration["JwtSettings:SecretKey"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
            }

            var key = Encoding.UTF8.GetBytes(secret);

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(15); 

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,        
                expires: expires,      
                signingCredentials: signingCredentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expires);
        }
    }
}
