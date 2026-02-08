using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;
using WebApplication3.Services;
using WebApplication3.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;

namespace FinancialWebApplication.Tests.Unit.Services
{
    /// <summary>
    /// Тесты для <see cref="TokenService"/>.
    /// </summary>
    public class TokenServiceTests
    {
        /// <summary>
        /// Мок конфигурации приложения.
        /// </summary>
        private readonly Mock<IConfiguration> _mockConfiguration;

        /// <summary>
        /// Экземпляр тестируемого сервиса токенов.
        /// </summary>
        private readonly TokenService _tokenService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TokenServiceTests"/>.
        /// </summary>
        public TokenServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();

            var secretKey = "SuperLongTestSecretKeyForJWTSigning1234567890123456";

            _mockConfiguration.Setup(x => x["JwtSettings:SecretKey"])
                .Returns(secretKey);
            _mockConfiguration.Setup(x => x["JwtSettings:Issuer"])
                .Returns("TestIssuer");
            _mockConfiguration.Setup(x => x["JwtSettings:Audience"])
                .Returns("TestAudience");

            _tokenService = new TokenService(_mockConfiguration.Object);
        }

        /// <summary>
        /// Проверяет успешное создание JWT-токена для валидного пользователя.
        /// </summary>
        [Fact]
        public void GenerateToken_ValidUser_ReturnsTokenAndExpiration()
        {
            var user = new User
            {
                Id = 1,
                Name = "Иван Иванов",
                Email = "ivan@example.com",
                Role = "User"
            };

            var (token, expiresAt) = _tokenService.GenerateToken(user);

            token.Should().NotBeNullOrEmpty();
            expiresAt.Should().BeAfter(DateTime.UtcNow);
            expiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(1));
        }

        /// <summary>
        /// Проверяет, что токен для пользователя с ролью Admin содержит корректные claims.
        /// </summary>
        [Fact]
        public void GenerateToken_UserWithAdminRole_ContainsCorrectRoleClaim()
        {
            var user = new User
            {
                Id = 1,
                Name = "Админ",
                Email = "admin@example.com",
                Role = "Admin"
            };

            var (token, _) = _tokenService.GenerateToken(user);

            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Role && c.Value == "Admin");

            jwtToken.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Email && c.Value == "admin@example.com");
        }

        /// <summary>
        /// Проверяет, что при отсутствии секретного ключа выбрасывается исключение <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void GenerateToken_MissingSecretKey_ThrowsException()
        {
            var invalidConfig = new Mock<IConfiguration>();
            invalidConfig.Setup(x => x["JwtSettings:SecretKey"]).Returns((string)null);

            var invalidService = new TokenService(invalidConfig.Object);
            var user = new User { Id = 1, Name = "Test", Email = "test@test.com", Role = "User" };

            Assert.Throws<InvalidOperationException>(() =>
                invalidService.GenerateToken(user));
        }

        /// <summary>
        /// Проверяет, что при слишком коротком секретном ключе выбрасывается исключение <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void GenerateToken_TooShortSecretKey_ThrowsException()
        {
            var shortKeyConfig = new Mock<IConfiguration>();

            shortKeyConfig.Setup(x => x["JwtSettings:SecretKey"])
                .Returns("ShortKeyLessThan32Chars");

            shortKeyConfig.Setup(x => x["JwtSettings:Issuer"])
                .Returns("Test");
            shortKeyConfig.Setup(x => x["JwtSettings:Audience"])
                .Returns("Test");

            var service = new TokenService(shortKeyConfig.Object);
            var user = new User { Id = 1, Name = "Test", Email = "test@test.com", Role = "User" };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.GenerateToken(user));
        }

        /// <summary>
        /// Проверяет, что токен успешно создается при минимально допустимой длине секретного ключа (32 символа).
        /// </summary>
        [Fact]
        public void GenerateToken_MinimumLengthSecretKey_Works()
        {
            var minKeyConfig = new Mock<IConfiguration>();

            minKeyConfig.Setup(x => x["JwtSettings:SecretKey"])
                .Returns("Exactly32CharactersLongSecretKey1234");

            minKeyConfig.Setup(x => x["JwtSettings:Issuer"])
                .Returns("Test");
            minKeyConfig.Setup(x => x["JwtSettings:Audience"])
                .Returns("Test");

            var service = new TokenService(minKeyConfig.Object);
            var user = new User { Id = 1, Name = "Test", Email = "test@test.com", Role = "User" };

            var (token, _) = service.GenerateToken(user);

            token.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Проверяет успешное создание валидного refresh-токена.
        /// </summary>
        [Fact]
        public void GenerateRefreshToken_ReturnsValidRefreshToken()
        {
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.Should().NotBeNull();
            refreshToken.Token.Should().NotBeNullOrEmpty();
            refreshToken.Expires.Should().BeAfter(DateTime.UtcNow);
            refreshToken.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Проверяет, что при многократном вызове создаются разные refresh-токены.
        /// </summary>
        [Fact]
        public void GenerateRefreshToken_MultipleCalls_GenerateDifferentTokens()
        {
            var token1 = _tokenService.GenerateRefreshToken();
            var token2 = _tokenService.GenerateRefreshToken();

            token1.Token.Should().NotBe(token2.Token);
        }

        /// <summary>
        /// Проверяет корректность времени истечения срока действия refresh-токена.
        /// </summary>
        [Fact]
        public void GenerateRefreshToken_TokenHasCorrectExpiration()
        {
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.Expires.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(30),
                TimeSpan.FromSeconds(1));
        }
    }
}