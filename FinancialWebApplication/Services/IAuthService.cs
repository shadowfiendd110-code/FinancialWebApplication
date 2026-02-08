using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication3.Data;
using WebApplication3.DTOs.Auth;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Repositories;

namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс аутентификации.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Регистрирует пользователя.
        /// </summary>
        /// <param name="registerUserDto">Регистрационное DTO.</param>
        Task<RegisterResponseUserDto> RegistrateUser(RegisterUserDto registerUserDto);

        /// <summary>
        /// Логинит пользователя.
        /// </summary>
        /// <param name="loginUserDto">Логин DTO пользователя.</param>
        /// <returns>Залогиненного пользователя с JWT-токеном.</returns>
        Task<AuthResultDto> LoginUser(LoginUserDto loginUserDto);

        /// <summary>
        /// Обновляет токены пользователя.
        /// </summary>
        /// <param name="refreshTokenDto">Рефреш токен.</param>
        /// <returns>Обновленные токены пользователя.</returns>
        Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

        /// <summary>
        /// Логаут пользователя.
        /// </summary>
        /// <param name="refreshTokenDto">DTO рефреш токена.</param>
        /// <param name="user">Пользователь</param>
        Task LogoutUser(RefreshTokenDto refreshTokenDto, ClaimsPrincipal user);
    }

    /// <summary>
    /// Сервис аутентификации.
    /// </summary>
    public class AuthService: IAuthService
    {
        /// <summary>
        /// Репозиторий для работы с пользователями.
        /// </summary>
        private IUserRepository _userRepository;

        /// <summary>
        /// Сервис для работы с паролями.
        /// </summary>
        private IPasswordHasherService _passwordHasher;

        /// <summary>
        /// Сервис для работы с токенами.
        /// </summary>
        private ITokenService _tokenService;

        /// <summary>
        /// Контекст для работы с бд.
        /// </summary>
        private ApplicationDbContext _context { get; set; }

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Инициализация сервиса аутентификации.
        /// </summary>
        /// <param name="userRepository">Репозиторий для работы с пользователями.</param>
        /// <param name="passwordHasher">Сервис для работы с паролями.</param>
        /// <param name="tokenService">Сервис для работы с токенами.</param>
        /// <param name="context">Контекст для работы с бд.</param>
        /// <param name="logger">Логгер сервиса.</param>
        public AuthService(IUserRepository userRepository, IPasswordHasherService passwordHasher, 
        ITokenService tokenService, ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Производит логаут пользователя.
        /// </summary>
        /// <param name="refreshTokenDto">Рефреш токен.</param>
        /// <param name="user">Пользователь.</param>
        /// <exception cref="UserClaimNotFoundException">Если пользователь не найден.</exception>
        public async Task LogoutUser(RefreshTokenDto refreshTokenDto, ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (idClaim is null)
            {
                throw new UserClaimNotFoundException(ClaimTypes.NameIdentifier);
            }

            _logger.LogInformation("Выход пользователя. UserId: {UserId}", idClaim.Value);

            var userId = int.Parse(idClaim.Value);

            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && rt.UserId == userId);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Пользователь вышел. UserId: {UserId}", idClaim.Value);
        }

        /// <summary>
        /// Логинит пользователя.
        /// </summary>
        /// <param name="loginUserDto">Логин DTO пользователя.</param>
        /// <returns>Залогиненного пользователя с JWT-токеном.</returns>
        public async Task<AuthResultDto> LoginUser(LoginUserDto loginUserDto)
        {
            _logger.LogInformation("Вход пользователя. Email: {Email}", loginUserDto.Email);

            var user = await _userRepository.FindByEmail(loginUserDto.Email);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            var passwordValid = _passwordHasher.VerifyHashedPassword(user.PasswordHash, loginUserDto.Password);

            if (!passwordValid)
            {
                _logger.LogWarning("Неудачная попытка входа. Email: {Email}, Причина: {Reason}", user.Email, "Неверный пароль");
                throw new InvalidPasswordException();
            }

            var tokenResult = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.User = user;
            user.RefreshTokens.Add(refreshToken);
            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Пользователь вошёл. UserId: {UserId}", user.Id);

            return new AuthResultDto
            {
                AccessToken = tokenResult.Token,
                RefreshToken = refreshToken.Token,
                Name = user.Name,
                Id = user.Id,
                Email = user.Email,
                ExpiresAt = tokenResult.ExpiresAt,
            };
        }

        /// <summary>
        /// Обновляет токены пользователя.
        /// </summary>
        /// <param name="refreshTokenDto">Входной DTO рефреш токена пользователя.</param>
        /// <returns></returns>
        public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

            if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow || 
                refreshToken.IsRevoked)
            {
                throw new InvalidRefreshTokenException();
            }

            var user = refreshToken.User;

            refreshToken.IsRevoked = true;
            _context.RefreshTokens.Update(refreshToken);

            var newAccessToken = _tokenService.GenerateToken(user);
            var newRefreshTokenObj = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("Обновление токенов");

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenObj.Token,
                Expires = newRefreshTokenObj.Expires,
                Created = newRefreshTokenObj.Created,
                IsRevoked = false,
            };

            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Токены обновлены для UserId: {UserId}", user.Id);

            return new AuthResultDto
            {
                AccessToken = newAccessToken.Token,
                RefreshToken = newRefreshToken.Token,  
                ExpiresAt = newAccessToken.ExpiresAt,  
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }

        /// <summary>
        /// Регистрирует пользователя.
        /// </summary>
        /// <param name="registrationUserDto">Регистрационное DTO.</param>
        public async Task<RegisterResponseUserDto> RegistrateUser(RegisterUserDto registrationUserDto)
        {
            var existingUser = await _userRepository.FindByEmail(registrationUserDto.Email);

            if (existingUser != null)
            {
                throw new ConflictException($"Пользователь с E-mail: {registrationUserDto.Email} уже существует");
            }

            _logger.LogInformation("Регистрация пользователя. Email: {Email}", registrationUserDto.Email);

            var passwordHash = _passwordHasher.HashPassword(registrationUserDto.Password);

            var user = new User
            {
                Name = registrationUserDto.Name,
                Email = registrationUserDto.Email,
                PasswordHash = passwordHash,
                Role = "User",
            };

            var registeredUser = await _userRepository.AddUser(user);

            var token = _tokenService.GenerateToken(registeredUser);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("Пользователь зарегистрирован. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

            return new RegisterResponseUserDto
            {
                Id = registeredUser.Id,
                Name = registeredUser.Name,
                Email = registeredUser.Email,
                Role = registeredUser.Role,
                AccessToken = token.Token,
                RefreshToken = refreshToken.Token,
                ExpiresAt = token.ExpiresAt,
            };
        }
    }
}
