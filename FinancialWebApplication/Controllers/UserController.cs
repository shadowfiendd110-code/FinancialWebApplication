using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.User;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Контроллер для работы с пользователями.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// Сервис для работы с пользователями.
        /// </summary>
        public IUserService _userService;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Помощник работы с кэшем.
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Создание контроллера.
        /// </summary>
        /// <param name="getService">Сервис для работы с пользователями.</param>
        /// <param name="logger">Логгер сервиса.</param>
        /// <param name="cache">Помощник работы с кэшем.</param>
        public UserController(IUserService getService, ILogger<UserController> logger, IMemoryCache cache)
        {
            _userService = getService;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Обновляет пароль пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="changePasswordDto">DTO смены пароля.</param>
        [HttpPut("{userId}/change-password")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> ChangeUserPassword(int userId, ChangePasswordDto changePasswordDto)
        {
            var currentUserId = GetCurrentUserId();

            _logger.LogInformation("HTTP PUT /api/User/{UserId}/change-password. Новый пароль у UserId: {UserId}",
                userId,
                userId);

            await _userService.ChangeUserPassword(userId, changePasswordDto);

            _logger.LogInformation(
                "HTTP 200 для PUT /api/User/{UserId}/change-password",
                userId);

            return Ok(new
            {
                message = "Пароль успешно изменен",
                changedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Получение Id текущего пользователя.
        /// </summary>
        /// <returns>Возвращает Id текущего пользователя.</returns>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        /// <summary>
        /// Создаёт пользователя.
        /// </summary>
        /// <param name="user">Входной DTO создаваемого пользователя.</param>
        /// <returns>Созданного пользователя.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser(CreateUserInputDto user)
        {
            _logger.LogInformation(
                "HTTP POST /api/User. Email: {UserEmail}",
                user.Email);

            var newUser = await _userService.CreateUser(user);

            _logger.LogInformation(
                "HTTP 201 для POST /api/User. UserId: {UserId}, Email: {UserEmail}",
                newUser.Id,
                newUser.Email);

            return Created($"api/user/{newUser.Id}", newUser);
        }

        /// <summary>
        /// Обновляет пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="user">DTO пользователя.</param>
        /// <returns>Обновлённого пользователя.</returns>
        [HttpPut("{userId}")]
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> UpdateUser(int userId, UserDto user)
        {
            _logger.LogInformation(
                "HTTP PUT /api/User/{UserId}. Новые данные: Имя='{UserName}', Email='{UserEmail}'",
                userId,
                user.Name,
                user.Email);

            var updatedUser = await _userService.UpdateUserProfile(userId, user);

            _logger.LogInformation(
                "HTTP 200 для PUT /api/User/{UserId}",
                userId);

            return Ok(updatedUser);
        }

        /// <summary>
        /// Получает всех пользователей.
        /// </summary>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для пользователей.</param>
        /// <returns>Всех пользователей.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] UserFilter? filter = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                return BadRequest("Номер страницы должна быть больше 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Размер страницы должен быть от 1 до 100");
            }

            if (filter != null)
            {
                if (filter.HasRoleFilter && filter.RoleEquals!.Length > 100)
                {
                    return BadRequest("Длина роли пользователя не может быть длинне 100 символов");
                }

                if (filter.HasNameFilter && filter.NameEquals!.Length > 100)
                {
                    return BadRequest("Длина имени пользователя не может быть длинне 100 символов");
                }

                if (filter.HasEmailFilter && filter.EmailEquals!.Length > 100)
                {
                    return BadRequest("Длина E-mail пользователя не может быть длинне 100 символов");
                }
            }

            var cacheKey = $"users_page_{pageNumber}_size_{pageSize}";

            if (filter != null)
            {
                if (filter.HasNameFilter)
                    cacheKey += $"_name_{filter.NameEquals}";
                if (filter.HasEmailFilter)
                    cacheKey += $"_email_{filter.EmailEquals}";
                if (filter.HasRoleFilter)
                    cacheKey += $"_role_{filter.RoleEquals}";
            }

            _logger.LogInformation("HTTP GET /api/User&pageNumber={PageNumber}&pageSize={PageSize}",
                pageNumber,
                pageSize);

            if (_cache.TryGetValue(cacheKey, out var cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Кэш: Взяли пользователей страница {PageNumber}, размер {PageSize}",
                    pageNumber,
                    pageSize);

                return Ok(cachedResult);
            }

            var pagedResponse = await _userService.GetAllUsers(filter, pageNumber, pageSize);

            _cache.Set(cacheKey, pagedResponse, TimeSpan.FromMinutes(5));

            _logger.LogInformation(
                "HTTP 200 для GET /api/User. " +
                "Страница: {PageNumber} из {TotalPages}. " +
                "Количество пользователей: {UsersCount}",
                pageNumber,
                pagedResponse.TotalPages,
                pagedResponse.Data.Count);

            return Ok(pagedResponse);
        }

        /// <summary>
        /// Получает пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>Пользователя.</returns>
        [HttpGet("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(int userId)
        {
            _logger.LogInformation("HTTP GET /api/User/{UserId}", userId);

            var user = await _userService.GetUser(userId);

            _logger.LogInformation("HTTP 200 для GET /api/User/{UserId}", userId);

            return Ok(user);
        }

        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            _logger.LogInformation("HTTP DELETE /api/User/{UserId}", userId);

            await _userService.DeleteUser(userId);

            _logger.LogInformation("HTTP 204 для DELETE /api/User/{UserId}", userId);

            return NoContent();
        }
    }
}