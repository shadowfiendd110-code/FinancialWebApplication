using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.User;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Models.Common;
using WebApplication3.Repositories;

namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс для работы с пользователем.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Создаёт пользователя.
        /// </summary>
        /// <param name="userDto">Входной DTO создаваемого пользователя.</param>
        /// <returns>Созданного пользователя.</returns>
        Task<CreatedUserOutputDto> CreateUser(CreateUserInputDto userDto);

        /// <summary>
        /// Получает всех пользователей.
        /// </summary>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для пользователей. </param>
        /// <returns>Всех пользователей.</returns>
        Task<PagedResponse<UserDto>> GetAllUsers(UserFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Получает пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <returns>Пользователя.</returns>
        Task<UserDto> GetUser(int id);

        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        Task DeleteUser(int id);

        /// <summary>
        /// Обновляет профиль пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <param name="userDto">DTO пользователя.</param>
        /// <returns>Обновлённого пользователя.</returns>
        Task<UpdateUserDto> UpdateUserProfile(int id, UserDto userDto);

        /// <summary>
        /// Обновляет пароль пользователя.
        /// </summary>
        /// <param name="changePasswordDto">DTO смены пароля.</param>
        /// <param name="userId">Id пользователя.</param>
        Task ChangeUserPassword(int userId, ChangePasswordDto changePasswordDto);
    }

    /// <summary>
    /// Сервис для работы с пользователями.
    /// </summary>
    public class UserService : IUserService
    {

        /// <summary>
        /// Репозиторий для работы с пользователями.
        /// </summary>
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// Хэшер паролей.
        /// </summary>
        private readonly IPasswordHasherService _passwordHasher;

        /// <summary>
        /// Создание сервиса.
        /// </summary>
        /// <param name="userRepository">Репозиторий для работы с пользователями.</param>
        /// <param name="logger">Логгер сервиса.</param>
        /// <param name="passwordHasher">Хэшер паролей.</param>
        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IPasswordHasherService passwordHasher)
        {
            _userRepository = userRepository;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Обновляет пароль пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="changePasswordDto">DTO смены пароля.</param>
        /// <exception cref="InvalidPasswordException">Выбрасывается в случае неверного пароля.</exception>
        public async Task ChangeUserPassword(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetUser(userId);

            var isCurrentPasswordValid = _passwordHasher.VerifyHashedPassword(
                user.PasswordHash, 
                changePasswordDto.CurrentPassword);

            if(!isCurrentPasswordValid)
            {
                throw new InvalidPasswordException("Неверный пароль.");
            }

            var newHashPassword = _passwordHasher.HashPassword(changePasswordDto.NewPassword);

            user.PasswordHash = newHashPassword;

            await _userRepository.ChangeUserPassword(user, newHashPassword);
        }

        /// <summary>
        /// Обновляет профиль пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <param name="userDto">DTO пользователя.</param>
        /// <returns>Обновлённого пользователя.</returns>
        public async Task<UpdateUserDto> UpdateUserProfile(int id, UserDto userDto)
        {
            var updatedUser = new User
            {
                Id = id,
                Name = userDto.Name,
                Email = userDto.Email,
            };

            _logger.LogInformation("Обновляем пользователя с {UserId}", id);

            var updateProfileUser = await _userRepository.UpdateUserProfile(updatedUser);

            _logger.LogInformation("Пользователь {UserId} обновлён. Имя: {UserName}, Email: {UserEmail}",
                updateProfileUser.Id, 
                updateProfileUser.Name,
                updateProfileUser.Email);

            return new UpdateUserDto
            {
                Id = updateProfileUser.Id,
                Name = updateProfileUser.Name,
                Email = updateProfileUser.Email,
            };
        }

        /// <summary>
        /// Создаёт пользователя.
        /// </summary>
        /// <param name="createdUserInputDto">Входной DTO создаваемого пользователя.</param>
        /// <returns>Созданного пользователя.</returns>
        public async Task<CreatedUserOutputDto> CreateUser(CreateUserInputDto createdUserInputDto)
        {
            var passwordHash = _passwordHasher.HashPassword(createdUserInputDto.Password);

            var newUser = new User
            {
                Name = createdUserInputDto.Name,
                Email = createdUserInputDto.Email,
                PasswordHash = passwordHash,
                Role = "User",
            };

            _logger.LogInformation("Добавление пользователя. Имя: {UserName} Email: {UserEmail}", 
                newUser.Name, 
                newUser.Email);

            var addUser = await _userRepository.AddUser(newUser);

            _logger.LogInformation("Добавлен пользователь. Имя: {UserName} Email: {UserEmail} Id: {UserId}", 
                addUser.Name, 
                addUser.Email, 
                addUser.Id);

            return new CreatedUserOutputDto
            {
                Id = addUser.Id,
                Name = addUser.Name,
                Email = addUser.Email,
            };
        }

        /// <summary>
        /// Получает пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        /// <returns>Пользователя.</returns>
        public async Task<UserDto> GetUser(int id)
        {
            _logger.LogInformation("Получение пользователя по UserId: {UserId}", id);

            var user = await _userRepository.GetUser(id);

            if (user == null)
            {
                _logger.LogWarning("Пользователь с UserId: {UserId} не найден", id);
                throw new UserNotFoundException($"Пользователь с ID {id} не найден");
            }

            _logger.LogInformation("Получен пользователь. Имя: {UserName} Email: {UserEmail}",
                user.Name,
                user.Email);

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
            };
        }

        /// <summary>
        /// Получает всех пользователей.
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Всех пользователей.</returns>
        public async Task<PagedResponse<UserDto>> GetAllUsers(UserFilter? filter = null, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Получение всех пользователей. " +
            "Фильтр: {Filter} Количество: {PageSize} Страница: {PageNumber}",
                filter,
                pageSize,
                pageNumber);

            var (users, totalCount) = await _userRepository.GetAllUsers(filter, pageNumber, pageSize);

            _logger.LogInformation("Получено {UsersCount} пользователей. Количество пользователей: {TotalUsersCount}", 
                users.Count,
                totalCount);

            var usersDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
            }).ToList();

            return new PagedResponse<UserDto>(
                usersDtos,
                pageNumber,
                pageSize,
                totalCount);
        }

        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        /// <param name="id">Id пользователя.</param>
        public async Task DeleteUser(int id)
        {
            _logger.LogInformation("Удаляем пользователя по UserId: {UserId}", id);

            await _userRepository.DeleteUser(id);

            _logger.LogInformation("Удалён пользователь с UserId: {UserId}", id);
        }
    }
}
