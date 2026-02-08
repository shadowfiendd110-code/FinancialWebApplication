using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.DTOs;
using WebApplication3.Services;
using WebApplication3.Controllers;
using WebApplication3.DTOs.Auth;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Контроллер аутентификации пользователя.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Сервис аутентификации.
        /// </summary>
        private readonly IAuthService _authService;

        /// <summary>
        /// Логгер контроллера.
        /// </summary>
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Инициализация контроллера.
        /// </summary>
        /// <param name="authService">Сервис аутентификации.</param>
        /// <param name="logger">Логгер.</param>
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Логинит пользователя.
        /// </summary>
        /// <param name="loginUserDto">Логин DTO пользователя.</param>
        /// <returns>Залогиненного пользователя с JWT-токеном.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResultDto>> LoginUser([FromBody] LoginUserDto loginUserDto)
        {
            _logger.LogInformation("HTTP POST /api/auth/login. Email: {UserEmail}",
                loginUserDto.Email);

            var result = await _authService.LoginUser(loginUserDto);

            if (result == null)
            {
                _logger.LogWarning("HTTP 401 для /api/auth/login. Email: {UserEmail}",
                    loginUserDto.Email);
                return Unauthorized("Неверный логин/пароль");
            }

            _logger.LogInformation("HTTP 200 для /api/auth/login. UserId: {UserId}",
                result.Id);

            return Ok(result);
        }

        /// <summary>
        /// Логаут пользователя.
        /// </summary>
        /// <param name="refreshTokenDto">DTO рефреш токена.</param>
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutUser([FromBody] RefreshTokenDto refreshTokenDto)
        {
            _logger.LogInformation("HTTP POST /api/auth/logout");

            await _authService.LogoutUser(refreshTokenDto, User);

            _logger.LogInformation("HTTP 200 для /api/auth/logout");

            return Ok();
        }

        /// <summary>
        /// Обновляет токены пользователя.
        /// </summary>
        /// <param name="dto">Рефреш токен.</param>
        /// <returns>Обновленные токены пользователя.</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            _logger.LogInformation("HTTP POST /api/auth/refresh");

            var result = await _authService.RefreshTokenAsync(dto);

            if (result == null)
            {
                _logger.LogWarning("HTTP 401 для /api/auth/refresh");
                return Unauthorized("Неверный логин/пароль");
            }

            _logger.LogInformation("HTTP 200 для /api/auth/refresh. UserId: {UserId}",
                result.Id);

            return Ok(result);
        }

        /// <summary>
        /// Регистрирует пользователя.
        /// </summary>
        /// <param name="registerUserDto">Регистрационное DTO.</param>
        /// <returns>Зарегистрированного пользователя.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegisterResponseUserDto>> RegisterUser(
            [FromBody] RegisterUserDto registerUserDto)
        {
            _logger.LogInformation("HTTP POST /api/auth/register. Email: {UserEmail}",
                registerUserDto.Email);

            var result = await _authService.RegistrateUser(registerUserDto);

            if (result == null)
            {
                _logger.LogWarning("HTTP 400 для /api/auth/register. Email: {UserEmail}",
                    registerUserDto.Email);
                return BadRequest("Ошибка регистрации");
            }

            _logger.LogInformation(
                "HTTP 201 для /api/auth/register. UserId: {UserId}, Email: {UserEmail}",
                result.Id,
                result.Email);

            return Created($"api/auth/{result.Id}", result);
        }
    }
}