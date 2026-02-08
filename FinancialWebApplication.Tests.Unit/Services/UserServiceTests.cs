using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.User;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Repositories;
using WebApplication3.Services;


namespace FinancialWebApplication.Tests.Unit.Services
{
    /// <summary>
    /// Тесты для <see cref="UserService"/>.
    /// </summary>
    public class UserServiceTests
    {
        /// <summary>
        /// Мок репозитория пользователей.
        /// </summary>
        private readonly Mock<IUserRepository> _mockUserRepository;

        /// <summary>
        /// Мок логгера сервиса пользователей.
        /// </summary>
        private readonly Mock<ILogger<UserService>> _mockLogger;

        /// <summary>
        /// Мок сервиса хеширования паролей.
        /// </summary>
        private readonly Mock<IPasswordHasherService> _mockPasswordHasher;

        /// <summary>
        /// Экземпляр тестируемого сервиса пользователей.
        /// </summary>
        private readonly UserService _userService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserServiceTests"/>.
        /// </summary>
        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockPasswordHasher = new Mock<IPasswordHasherService>();

            _userService = new UserService(
                _mockUserRepository.Object,
                _mockLogger.Object,
                _mockPasswordHasher.Object
            );
        }


        /// <summary>
        /// Проверяет успешное получение существующего пользователя.
        /// </summary>
        [Fact]
        public async Task GetUser_ExistingUser_ReturnsUserDto()
        {
            var userId = 1;
            var userFromRepo = new User
            {
                Id = userId,
                Name = "Иван Иванов",
                Email = "ivan@example.com"
            };

            _mockUserRepository
                .Setup(r => r.GetUser(userId))
                .ReturnsAsync(userFromRepo);

            var result = await _userService.GetUser(userId);

            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
            result.Name.Should().Be("Иван Иванов");
            result.Email.Should().Be("ivan@example.com");

            _mockUserRepository.Verify(r => r.GetUser(userId), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при попытке получить несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetUser_NonExistingUser_ThrowsNotFoundException()
        {
            var userId = 999;
            User? nullUser = null;

            _mockUserRepository
                .Setup(r => r.GetUser(userId))
                .ReturnsAsync(nullUser!); 

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _userService.GetUser(userId));

            _mockUserRepository.Verify(r => r.GetUser(userId), Times.Once);
        }

        /// <summary>
        /// Проверяет успешное создание пользователя с валидными данными.
        /// </summary>
        [Fact]
        public async Task CreateUser_ValidData_CreatesAndReturnsUser()
        {
            var createDto = new CreateUserInputDto
            {
                Name = "Мария Петрова",
                Email = "maria@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var hashedPassword = "hashed_password_123";
            var createdUser = new User
            {
                Id = 1,
                Name = createDto.Name,
                Email = createDto.Email,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            _mockPasswordHasher
                .Setup(h => h.HashPassword(createDto.Password))
                .Returns(hashedPassword);

            _mockUserRepository
                .Setup(r => r.AddUser(It.Is<User>(u =>
                    u.Name == createDto.Name &&
                    u.Email == createDto.Email &&
                    u.PasswordHash == hashedPassword &&
                    u.Role == "User")))
                .ReturnsAsync(createdUser);

            var result = await _userService.CreateUser(createDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Мария Петрова");
            result.Email.Should().Be("maria@example.com");

            _mockPasswordHasher.Verify(h => h.HashPassword(createDto.Password), Times.Once);
            _mockUserRepository.Verify(r => r.AddUser(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// Проверяет успешное обновление профиля пользователя.
        /// </summary>
        [Fact]
        public async Task UpdateUserProfile_ValidData_UpdatesAndReturnsUser()
        {
            var userId = 1;
            var userDto = new UserDto
            {
                Id = userId,
                Name = "Обновлённое Имя",
                Email = "updated@example.com"
            };

            var updatedUser = new User
            {
                Id = userId,
                Name = userDto.Name,
                Email = userDto.Email
            };

            _mockUserRepository
                .Setup(r => r.UpdateUserProfile(It.Is<User>(u =>
                    u.Id == userId &&
                    u.Name == userDto.Name &&
                    u.Email == userDto.Email)))
                .ReturnsAsync(updatedUser);

            var result = await _userService.UpdateUserProfile(userId, userDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
            result.Name.Should().Be("Обновлённое Имя");
            result.Email.Should().Be("updated@example.com");

            _mockUserRepository.Verify(r => r.UpdateUserProfile(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// Проверяет успешную смену пароля пользователя при правильном текущем пароле.
        /// </summary>
        [Fact]
        public async Task ChangeUserPassword_ValidCurrentPassword_ChangesPassword()
        {
            var userId = 1;
            var currentHashedPassword = "old_hashed_password";
            var newHashedPassword = "new_hashed_password";

            var user = new User
            {
                Id = userId,
                PasswordHash = currentHashedPassword
            };

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword456!",
                ConfirmNewPassword = "NewPassword456!"
            };

            _mockUserRepository
                .Setup(r => r.GetUser(userId))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword(currentHashedPassword, changePasswordDto.CurrentPassword))
                .Returns(true);

            _mockPasswordHasher
                .Setup(h => h.HashPassword(changePasswordDto.NewPassword))
                .Returns(newHashedPassword);

            await _userService.ChangeUserPassword(userId, changePasswordDto);

            user.PasswordHash.Should().Be(newHashedPassword);

            _mockPasswordHasher.Verify(h =>
                h.VerifyHashedPassword(currentHashedPassword, changePasswordDto.CurrentPassword),
                Times.Once);

            _mockPasswordHasher.Verify(h =>
                h.HashPassword(changePasswordDto.NewPassword),
                Times.Once);

            _mockUserRepository.Verify(r =>
                r.ChangeUserPassword(user, newHashedPassword),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что при попытке смены пароля с неверным текущим паролем выбрасывается исключение <see cref="InvalidPasswordException"/>.
        /// </summary>
        [Fact]
        public async Task ChangeUserPassword_InvalidCurrentPassword_ThrowsInvalidPasswordException()
        {
            var userId = 1;
            var currentHashedPassword = "old_hashed_password";

            var user = new User
            {
                Id = userId,
                PasswordHash = currentHashedPassword
            };

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword!",
                NewPassword = "NewPassword456!",
                ConfirmNewPassword = "NewPassword456!"
            };

            _mockUserRepository
                .Setup(r => r.GetUser(userId))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword(currentHashedPassword, changePasswordDto.CurrentPassword))
                .Returns(false);

            await Assert.ThrowsAsync<InvalidPasswordException>(() =>
                _userService.ChangeUserPassword(userId, changePasswordDto));

            _mockPasswordHasher.Verify(h =>
                h.HashPassword(It.IsAny<string>()),
                Times.Never);

            _mockUserRepository.Verify(r =>
                r.ChangeUserPassword(It.IsAny<User>(), It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Проверяет получение всех пользователей без фильтра с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_NoFilter_ReturnsPagedUsers()
        {
            var pageNumber = 1;
            var pageSize = 10;

            var usersFromRepo = new List<User>
            {
                new User { Id = 1, Name = "Иван", Email = "ivan@test.com" },
                new User { Id = 2, Name = "Мария", Email = "maria@test.com" }
            };

            var totalCount = 2;

            _mockUserRepository
                .Setup(r => r.GetAllUsers(null, pageNumber, pageSize))
                .ReturnsAsync((usersFromRepo, totalCount));

            var result = await _userService.GetAllUsers(null, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2); 
            result.PageNumber.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);
            result.TotalRecords.Should().Be(totalCount); 

            result.Data[0].Id.Should().Be(1); 
            result.Data[0].Name.Should().Be("Иван"); 

            result.Data[1].Id.Should().Be(2); 
            result.Data[1].Name.Should().Be("Мария"); 

            _mockUserRepository.Verify(r =>
                r.GetAllUsers(null, pageNumber, pageSize),
                Times.Once);
        }

        /// <summary>
        /// Проверяет получение пользователей с применением фильтра.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithFilter_ReturnsFilteredUsers()
        {
            var filter = new UserFilter { NameEquals = "Иван" };
            var pageNumber = 1;
            var pageSize = 10;

            var usersFromRepo = new List<User>
            {
                new User { Id = 1, Name = "Иван", Email = "ivan@test.com" }
            };

            var totalCount = 1;

            _mockUserRepository
                .Setup(r => r.GetAllUsers(filter, pageNumber, pageSize))
                .ReturnsAsync((usersFromRepo, totalCount));

            var result = await _userService.GetAllUsers(filter, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1); 
            result.TotalRecords.Should().Be(1); 

            result.Data[0].Name.Should().Be("Иван"); 

            _mockUserRepository.Verify(r =>
                r.GetAllUsers(filter, pageNumber, pageSize),
                Times.Once);
        }

        /// <summary>
        /// Проверяет успешное удаление существующего пользователя.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ExistingUser_DeletesUser()
        {
            var userId = 1;

            _mockUserRepository
                .Setup(r => r.DeleteUser(userId))
                .Returns(Task.CompletedTask);

            await _userService.DeleteUser(userId);

            _mockUserRepository.Verify(r => r.DeleteUser(userId), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при попытке удаления несуществующего пользователя репозиторий всё равно вызывается.
        /// </summary>
        [Fact]
        public async Task DeleteUser_NonExistingUser_StillCallsRepository()
        {
            var userId = 999;

            _mockUserRepository
                .Setup(r => r.DeleteUser(userId))
                .Returns(Task.CompletedTask);

            await _userService.DeleteUser(userId);

            _mockUserRepository.Verify(r => r.DeleteUser(userId), Times.Once);
        }
    }
}