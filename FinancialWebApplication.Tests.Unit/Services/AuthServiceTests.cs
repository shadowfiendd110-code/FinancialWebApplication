using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Services;
using WebApplication3.Repositories;
using WebApplication3.Models;
using System.Security.Claims;
using FluentAssertions;
using WebApplication3.Exceptions;
using WebApplication3.Data;
using WebApplication3.DTOs.Auth;

namespace FinancialWebApplication.Tests.Unit.Services
{
    /// <summary>
    /// Тесты для <see cref="AuthService"/>.
    /// </summary>
    public class AuthServiceTests
    {
        /// <summary>
        /// Мок репозитория пользователей.
        /// </summary>
        private readonly Mock<IUserRepository> _mockUserRepository;

        /// <summary>
        /// Мок сервиса хеширования паролей.
        /// </summary>
        private readonly Mock<IPasswordHasherService> _mockPasswordHasher;

        /// <summary>
        /// Мок сервиса токенов.
        /// </summary>
        private readonly Mock<ITokenService> _mockTokenService;

        /// <summary>
        /// Логгер для сервиса пользователей.
        /// </summary>
        private readonly ILogger<AuthService> _mockLogger;

        /// <summary>
        /// Контекст базы данных для тестирования.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Экземпляр тестируемого сервиса аутентификации.
        /// </summary>
        private readonly AuthService _authService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AuthServiceTests"/>.
        /// </summary>
        public AuthServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPasswordHasher = new Mock<IPasswordHasherService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = Mock.Of<ILogger<AuthService>>(); 

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"mydatabase_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);

            _authService = new AuthService(
                _mockUserRepository.Object,
                _mockPasswordHasher.Object,
                _mockTokenService.Object,
                _context,
                _mockLogger 
            );
        }

        /// <summary>
        /// Проверяет успешный вход пользователя с валидными учетными данными.
        /// </summary>
        [Fact]
        public async Task LoginUser_ValidCredentials_ReturnsAuthResult()
        {
            var loginDto = new LoginUserDto
            {
                Email = "user@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = 1,
                Name = "Иван Иванов",
                Email = loginDto.Email,
                PasswordHash = "hashed_password",
                Role = "User"
            };

            var tokenResult = ("jwt_token_here", DateTime.UtcNow.AddMinutes(15));
            var refreshToken = new RefreshToken
            {
                Token = "refresh_token_here",
                Expires = DateTime.UtcNow.AddDays(30),
                Created = DateTime.UtcNow
            };

            _mockUserRepository
                .Setup(r => r.FindByEmail(loginDto.Email))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword(user.PasswordHash, loginDto.Password))
                .Returns(true);

            _mockTokenService
                .Setup(t => t.GenerateToken(user))
                .Returns(tokenResult);

            _mockTokenService
                .Setup(t => t.GenerateRefreshToken())
                .Returns(refreshToken);

            var result = await _authService.LoginUser(loginDto);

            result.Should().NotBeNull();
            result.AccessToken.Should().Be("jwt_token_here");
            result.RefreshToken.Should().Be("refresh_token_here");
            result.Name.Should().Be("Иван Иванов");
            result.Email.Should().Be("user@example.com");
            result.Id.Should().Be(1);

            var savedToken = await _context.RefreshTokens.FirstOrDefaultAsync();
            savedToken.Should().NotBeNull();
            savedToken?.Token.Should().Be("refresh_token_here");

            _mockUserRepository.Verify(r => r.FindByEmail(loginDto.Email), Times.Once);
            _mockPasswordHasher.Verify(h =>
                h.VerifyHashedPassword(user.PasswordHash, loginDto.Password), Times.Once);
            _mockTokenService.Verify(t => t.GenerateToken(user), Times.Once);
            _mockTokenService.Verify(t => t.GenerateRefreshToken(), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при входе с несуществующим email выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task LoginUser_InvalidEmail_ThrowsUserNotFoundException()
        {
            // Arrange
            var loginDto = new LoginUserDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _mockUserRepository
                .Setup(r => r.FindByEmail(loginDto.Email))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _authService.LoginUser(loginDto));
        }

        /// <summary>
        /// Проверяет, что при входе с неверным паролем выбрасывается исключение <see cref="InvalidPasswordException"/>.
        /// </summary>
        [Fact]
        public async Task LoginUser_InvalidPassword_ThrowsInvalidPasswordException()
        {
            // Arrange
            var loginDto = new LoginUserDto
            {
                Email = "user@example.com",
                Password = "WrongPassword!"
            };

            var user = new User
            {
                Id = 1,
                Email = loginDto.Email,
                PasswordHash = "hashed_password"
            };

            _mockUserRepository
                .Setup(r => r.FindByEmail(loginDto.Email))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword(user.PasswordHash, loginDto.Password))
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidPasswordException>(() =>
                _authService.LoginUser(loginDto));
        }

        /// <summary>
        /// Проверяет успешную регистрацию пользователя с валидными данными.
        /// </summary>
        [Fact]
        public async Task RegistrateUser_ValidData_RegistersUser()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Name = "Новый Пользователь",
                Email = "myuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var hashedPassword = "hashed_password_123";
            var user = new User
            {
                Id = 1,
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            var tokenResult = ("jwt_token", DateTime.UtcNow.AddMinutes(15));
            var refreshToken = new RefreshToken
            {
                Token = "refresh_token"
            };

            _mockUserRepository
                .Setup(r => r.FindByEmail(registerDto.Email))
                .ReturnsAsync((User?)null);

            _mockPasswordHasher
                .Setup(h => h.HashPassword(registerDto.Password))
                .Returns(hashedPassword);

            _mockUserRepository
                .Setup(r => r.AddUser(It.Is<User>(u =>
                    u.Name == registerDto.Name &&
                    u.Email == registerDto.Email &&
                    u.PasswordHash == hashedPassword &&
                    u.Role == "User")))
                .ReturnsAsync(user);

            _mockTokenService
                .Setup(t => t.GenerateToken(user))
                .Returns(tokenResult);

            _mockTokenService
                .Setup(t => t.GenerateRefreshToken())
                .Returns(refreshToken);

            var result = await _authService.RegistrateUser(registerDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Новый Пользователь");
            result.Email.Should().Be(registerDto.Email);
            result.Role.Should().Be("User");
            result.AccessToken.Should().Be("jwt_token");
            result.RefreshToken.Should().Be("refresh_token");

            _mockUserRepository.Verify(r => r.FindByEmail(registerDto.Email), Times.Once);
            _mockUserRepository.Verify(r => r.AddUser(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при регистрации с существующим email выбрасывается исключение <see cref="ConflictException"/>.
        /// </summary>
        [Fact]
        public async Task RegistrateUser_ExistingEmail_ThrowsConflictException()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Email = "existing@example.com",
                Name = "Тест",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var existingUser = new User { Id = 1, Email = registerDto.Email };

            _mockUserRepository
                .Setup(r => r.FindByEmail(registerDto.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() =>
                _authService.RegistrateUser(registerDto));

            _mockUserRepository.Verify(r => r.AddUser(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// Проверяет успешное обновление токенов по валидному refresh-токену.
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewTokens()
        {
            var user = new User
            {
                Id = 1,
                Name = "Иван",
                Email = "ivan@example.com",
                Role = "User"
            };

            var existingRefreshToken = new RefreshToken
            {
                Id = 1,
                Token = "valid_refresh_token",
                UserId = user.Id,
                User = user,
                Expires = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };

            _context.Users.Add(user);
            _context.RefreshTokens.Add(existingRefreshToken);
            await _context.SaveChangesAsync();

            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "valid_refresh_token"
            };

            var newAccessToken = ("new_jwt_token", DateTime.UtcNow.AddMinutes(15));
            var newRefreshToken = new RefreshToken
            {
                Token = "new_refresh_token",
                Expires = DateTime.UtcNow.AddDays(30),
                Created = DateTime.UtcNow
            };

            _mockTokenService
                .Setup(t => t.GenerateToken(user))
                .Returns(newAccessToken);

            _mockTokenService
                .Setup(t => t.GenerateRefreshToken())
                .Returns(newRefreshToken);

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);

            result.Should().NotBeNull();
            result.AccessToken.Should().Be("new_jwt_token");
            result.RefreshToken.Should().Be("new_refresh_token");
            result.Name.Should().Be("Иван");
            result.Email.Should().Be("ivan@example.com");
            result.Id.Should().Be(1);

            var revokedToken = await _context.RefreshTokens.FindAsync(1);
            revokedToken.Should().NotBeNull();
            revokedToken?.IsRevoked.Should().BeTrue();

            var newToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == "new_refresh_token");
            newToken.Should().NotBeNull();
        }

        /// <summary>
        /// Проверяет, что при попытке обновления с истёкшим токеном выбрасывается исключение <see cref="InvalidRefreshTokenException"/>.
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ThrowsInvalidRefreshTokenException()
        {
            // Arrange
            var user = new User { Id = 1 };
            var expiredRefreshToken = new RefreshToken
            {
                Token = "expired_token",
                UserId = user.Id,
                User = user,
                Expires = DateTime.UtcNow.AddDays(-1), 
                IsRevoked = false
            };

            _context.Users.Add(user);
            _context.RefreshTokens.Add(expiredRefreshToken);
            await _context.SaveChangesAsync();

            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "expired_token"
            };

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                _authService.RefreshTokenAsync(refreshTokenDto));
        }

        /// <summary>
        /// Проверяет, что при попытке обновления с отозванным токеном выбрасывается исключение <see cref="InvalidRefreshTokenException"/>.
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_RevokedToken_ThrowsInvalidRefreshTokenException()
        {
            // Arrange
            var user = new User { Id = 1 };
            var revokedRefreshToken = new RefreshToken
            {
                Token = "revoked_token",
                UserId = user.Id,
                User = user,
                Expires = DateTime.UtcNow.AddDays(1),
                IsRevoked = true 
            };

            _context.Users.Add(user);
            _context.RefreshTokens.Add(revokedRefreshToken);
            await _context.SaveChangesAsync();

            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "revoked_token"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                _authService.RefreshTokenAsync(refreshTokenDto));
        }

        /// <summary>
        /// Проверяет успешный выход пользователя с отзывом refresh-токена.
        /// </summary>
        [Fact]
        public async Task LogoutUser_ValidUser_RevokesRefreshToken()
        {
            var userId = 1;
            var user = new User { Id = userId };

            var refreshToken = new RefreshToken
            {
                Id = 1,
                Token = "token_to_revoke",
                UserId = userId,
                User = user,
                IsRevoked = false
            };

            _context.Users.Add(user);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "token_to_revoke"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var userPrincipal = new ClaimsPrincipal(identity);

            await _authService.LogoutUser(refreshTokenDto, userPrincipal);

            var revokedToken = await _context.RefreshTokens.FindAsync(1);
            revokedToken.Should().NotBeNull();
            revokedToken?.IsRevoked.Should().BeTrue();
        }

        /// <summary>
        /// Проверяет, что при выходе без claim идентификатора пользователя выбрасывается исключение <see cref="UserClaimNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task LogoutUser_NoUserIdClaim_ThrowsUserClaimNotFoundException()
        {
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "some_token"
            };

            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "Test");
            var userPrincipal = new ClaimsPrincipal(identity);

            // Act & Assert
            await Assert.ThrowsAsync<UserClaimNotFoundException>(() =>
                _authService.LogoutUser(refreshTokenDto, userPrincipal));
        }

        /// <summary>
        /// Проверяет, что при выходе с несуществующим refresh-токеном не возникает исключений.
        /// </summary>
        [Fact]
        public async Task LogoutUser_RefreshTokenNotFound_DoesNotThrow()
        {
            var userId = 1;
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "nonexistent_token"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var userPrincipal = new ClaimsPrincipal(identity);

            await _authService.LogoutUser(refreshTokenDto, userPrincipal);

            var tokens = await _context.RefreshTokens.ToListAsync();
            tokens.Should().BeEmpty(); // В базе нет токенов
        }

        /// <summary>
        /// Освобождает ресурсы тестового класса.
        /// </summary>
        internal void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}