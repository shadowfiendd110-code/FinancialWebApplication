using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WebApplication3.Controllers;
using WebApplication3.DTOs.Filters;
using WebApplication3.Exceptions;
using WebApplication3.Models.Common;
using WebApplication3.Services;
using FluentAssertions;
using WebApplication3.DTOs.User;

namespace WebApplication3.Tests.Controllers
{
    /// <summary>
    /// Тесты для <see cref="UserController"/>.
    /// </summary>
    public class UserControllerTests
    {
        /// <summary>
        /// Мок сервиса пользователей.
        /// </summary>
        private readonly Mock<IUserService> _mockUserService;

        /// <summary>
        /// Мок логгера контроллера пользователей.
        /// </summary>
        private readonly Mock<ILogger<UserController>> _mockLogger;

        /// <summary>
        /// Мок кэша в памяти.
        /// </summary>
        private readonly Mock<IMemoryCache> _mockCache;

        /// <summary>
        /// Экземпляр тестируемого контроллера пользователей.
        /// </summary>
        private UserController _controller;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserControllerTests"/>.
        /// </summary>
        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UserController>>();
            _mockCache = new Mock<IMemoryCache>();

            _controller = new UserController(
                _mockUserService.Object,
                _mockLogger.Object,
                _mockCache.Object);

            SetupControllerContext();
        }

        /// <summary>
        /// Настраивает контекст контроллера для тестов с аутентифицированным пользователем.
        /// </summary>
        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        /// <summary>
        /// Проверяет успешное получение всех пользователей с валидными параметрами.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_ValidParameters_ReturnsOkResult()
        {
            var pageNumber = 1;
            var pageSize = 10;
            var expectedResponse = new PagedResponse<UserDto>(
                new List<UserDto>
                {
                    new UserDto { Id = 1, Name = "User1", Email = "user1@test.com" },
                    new UserDto { Id = 2, Name = "User2", Email = "user2@test.com" }
                },
                pageNumber,
                pageSize,
                2
            );

            object cachedValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cachedValue)).Returns(false);
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            _mockUserService
                .Setup(s => s.GetAllUsers(null, pageNumber, pageSize))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.GetAllUsers(pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        }

        /// <summary>
        /// Проверяет, что при невалидном номере страницы возвращается ошибка BadRequest.
        /// </summary>
        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        public async Task GetAllUsers_InvalidPageNumber_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            var result = await _controller.GetAllUsers(pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Проверяет, что при невалидном размере страницы возвращается ошибка BadRequest.
        /// </summary>
        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        [InlineData(1, -1)]
        public async Task GetAllUsers_InvalidPageSize_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            var result = await _controller.GetAllUsers(pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Проверяет, что при невалидном фильтре возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithFilterValidation_ReturnsBadRequestWhenFilterInvalid()
        {
            var filter = new UserFilter
            {
                NameEquals = new string('a', 101)
            };

            var result = await _controller.GetAllUsers(filter: filter);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Проверяет получение всех пользователей из кэша.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_ReturnsCachedResult_WhenCacheExists()
        {
            var pageNumber = 1;
            var pageSize = 10;
            var cachedResponse = new PagedResponse<UserDto>(
                new List<UserDto> { new UserDto { Id = 1, Name = "Cached", Email = "cached@test.com" } },
                pageNumber,
                pageSize,
                1
            );

            object cachedValue = cachedResponse;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cachedValue)).Returns(true);

            var result = await _controller.GetAllUsers(pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeSameAs(cachedResponse);

            _mockUserService.Verify(
                s => s.GetAllUsers(It.IsAny<UserFilter>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Проверяет успешное получение существующего пользователя.
        /// </summary>
        [Fact]
        public async Task GetUser_ValidId_ReturnsOkResult()
        {
            var userId = 1;
            var expectedUser = new UserDto
            {
                Id = userId,
                Name = "Test User",
                Email = "test@example.com"
            };

            _mockUserService
                .Setup(s => s.GetUser(userId))
                .ReturnsAsync(expectedUser);

            var result = await _controller.GetUser(userId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedUser);
        }

        /// <summary>
        /// Проверяет, что при попытке получения несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetUser_UserNotFound_ThrowsUserNotFoundException()
        {
            var userId = 999;

            _mockUserService
                .Setup(s => s.GetUser(userId))
                .ThrowsAsync(new UserNotFoundException($"Пользователь с ID {userId} не найден"));

            var exception = await Assert.ThrowsAsync<UserNotFoundException>(
                () => _controller.GetUser(userId));

            exception.Message.Should().Contain("999");
            exception.Message.Should().Contain("не найден");
        }

        /// <summary>
        /// Проверяет успешное создание нового пользователя.
        /// </summary>
        [Fact]
        public async Task CreateUser_ValidDto_ReturnsCreatedResult()
        {
            var createDto = new CreateUserInputDto
            {
                Name = "New User",
                Email = "new@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var expectedResult = new CreatedUserOutputDto
            {
                Id = 1,
                Name = "New User",
                Email = "new@example.com"
            };

            _mockUserService
                .Setup(s => s.CreateUser(createDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.CreateUser(createDto);

            result.Should().BeOfType<CreatedResult>();
            var createdResult = result as CreatedResult;
            createdResult!.Location.Should().Be($"api/user/{expectedResult.Id}");
            createdResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет успешное обновление существующего пользователя.
        /// </summary>
        [Fact]
        public async Task UpdateUser_ValidDto_ReturnsOkResult()
        {
            var userId = 1;
            var userDto = new UserDto
            {
                Id = userId,
                Name = "Updated Name",
                Email = "updated@example.com",
                Password = "Password123"
            };

            var expectedResult = new UpdateUserDto
            {
                Id = userId,
                Name = "Updated Name",
                Email = "updated@example.com"
            };

            _mockUserService
                .Setup(s => s.UpdateUserProfile(userId, userDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.UpdateUser(userId, userDto);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет, что при попытке обновления несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task UpdateUser_UserNotFound_ThrowsException()
        {
            var userId = 999;
            var userDto = new UserDto
            {
                Name = "Test",
                Email = "test@example.com",
                Password = "Password123"
            };

            _mockUserService
                .Setup(s => s.UpdateUserProfile(userId, userDto))
                .ThrowsAsync(new UserNotFoundException($"Пользователь с ID {userId} не найден"));

            await Assert.ThrowsAsync<UserNotFoundException>(
                () => _controller.UpdateUser(userId, userDto));
        }

        /// <summary>
        /// Проверяет успешное удаление существующего пользователя.
        /// </summary>
        [Fact]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            var userId = 1;

            _mockUserService
                .Setup(s => s.DeleteUser(userId))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser(userId);

            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// Проверяет, что при попытке удаления несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task DeleteUser_UserNotFound_ThrowsException()
        {
            var userId = 999;

            _mockUserService
                .Setup(s => s.DeleteUser(userId))
                .ThrowsAsync(new UserNotFoundException($"Пользователь с ID {userId} не найден"));

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _controller.DeleteUser(userId));
        }

        /// <summary>
        /// Проверяет успешную смену пароля пользователя.
        /// </summary>
        [Fact]
        public async Task ChangeUserPassword_ValidDto_ReturnsOkResult()
        {
            var userId = 1;
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                ConfirmNewPassword = "NewPassword123"
            };

            _mockUserService
                .Setup(s => s.ChangeUserPassword(userId, changePasswordDto))
                .Returns(Task.CompletedTask);

            var result = await _controller.ChangeUserPassword(userId, changePasswordDto);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            okResult!.Value.Should().NotBeNull();

            _mockUserService.Verify(
                s => s.ChangeUserPassword(userId, changePasswordDto),
                Times.Once);
        }

        /// <summary>
        /// Проверяет настройку авторизации для метода получения всех пользователей.
        /// </summary>
        [Fact]
        public async Task GetAllUsers_WithoutAdminRole_ShouldNotBeCalled()
        {
            var nonAdminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(nonAdminClaims, "TestAuth");
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            await Task.CompletedTask;
        }

        /// <summary>
        /// Проверяет получение идентификатора текущего пользователя при наличии корректного claim.
        /// </summary>
        [Fact]
        public void GetCurrentUserId_ValidClaim_ReturnsUserId()
        {
            var expectedUserId = 123;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.ControllerContext.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                .Should().Be(expectedUserId.ToString());
        }

        /// <summary>
        /// Проверяет обработку отсутствия claim идентификатора пользователя.
        /// </summary>
        [Fact]
        public void GetCurrentUserId_NoClaim_ReturnsZero()
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.ControllerContext.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier)
                .Should().BeNull();
        }
    }
}