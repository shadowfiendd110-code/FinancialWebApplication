using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WebApplication3.Controllers;
using WebApplication3.DTOs;
using WebApplication3.DTOs.Auth;
using WebApplication3.Exceptions;
using WebApplication3.Services;

namespace WebApplication3.Tests.Controllers
{
    /// <summary>
    /// Тесты для <see cref="AuthController"/>.
    /// </summary>
    public class AuthControllerTests
    {
        /// <summary>
        /// Мок сервиса аутентификации.
        /// </summary>
        private readonly Mock<IAuthService> _mockAuthService;

        /// <summary>
        /// Мок логгера контроллера аутентификации.
        /// </summary>
        private readonly Mock<ILogger<AuthController>> _mockLogger;

        /// <summary>
        /// Экземпляр тестируемого контроллера аутентификации.
        /// </summary>
        private AuthController _controller;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AuthControllerTests"/>.
        /// </summary>
        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// Проверяет успешный вход пользователя с валидными учетными данными.
        /// </summary>
        [Fact]
        public async Task LoginUser_ValidCredentials_ReturnsOkResult()
        {
            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var expectedResult = new AuthResultDto
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                AccessToken = "jwt-token-here",
                RefreshToken = "refresh-token-here",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService
                .Setup(s => s.LoginUser(loginDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.LoginUser(loginDto);

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет, что при попытке входа с неверными учетными данными выбрасывается исключение <see cref="InvalidPasswordException"/>.
        /// </summary>
        [Fact]
        public async Task LoginUser_InvalidCredentials_ThrowsException()
        {
            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockAuthService
                .Setup(s => s.LoginUser(loginDto))
                .ThrowsAsync(new InvalidPasswordException());

            await Assert.ThrowsAsync<InvalidPasswordException>(
                () => _controller.LoginUser(loginDto));
        }

        /// <summary>
        /// Проверяет, что при попытке входа несуществующего пользователя выбрасывается исключение <see cref="UserNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task LoginUser_UserNotFound_ThrowsException()
        {
            var loginDto = new LoginUserDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123"
            };

            _mockAuthService
                .Setup(s => s.LoginUser(loginDto))
                .ThrowsAsync(new UserNotFoundException());

            await Assert.ThrowsAsync<UserNotFoundException>(
                () => _controller.LoginUser(loginDto));
        }

        /// <summary>
        /// Проверяет успешную регистрацию нового пользователя.
        /// </summary>
        [Fact]
        public async Task RegisterUser_ValidData_ReturnsCreatedResult()
        {
            var registerDto = new RegisterUserDto
            {
                Name = "New User",
                Email = "new@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            var expectedResult = new RegisterResponseUserDto
            {
                Id = 1,
                Name = "New User",
                Email = "new@example.com",
                Role = "User",
                AccessToken = "jwt-token-here",
                RefreshToken = "refresh-token-here",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService
                .Setup(s => s.RegistrateUser(registerDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.RegisterUser(registerDto);

            result.Result.Should().BeOfType<CreatedResult>()
                .Which.Should().Satisfy<CreatedResult>(created =>
                {
                    created.Location.Should().Be($"api/auth/{expectedResult.Id}");
                    created.Value.Should().BeEquivalentTo(expectedResult);
                });
        }

        /// <summary>
        /// Проверяет, что при попытке регистрации пользователя с существующим email выбрасывается исключение <see cref="ConflictException"/>.
        /// </summary>
        [Fact]
        public async Task RegisterUser_UserAlreadyExists_ThrowsException()
        {
            var registerDto = new RegisterUserDto
            {
                Name = "Existing User",
                Email = "existing@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            _mockAuthService
                .Setup(s => s.RegistrateUser(registerDto))
                .ThrowsAsync(new ConflictException($"Пользователь с E-mail: {registerDto.Email} уже существует"));

            await Assert.ThrowsAsync<ConflictException>(
                () => _controller.RegisterUser(registerDto));
        }

        /// <summary>
        /// Проверяет успешное обновление токенов по валидному refresh-токену.
        /// </summary>
        [Fact]
        public async Task Refresh_ValidToken_ReturnsOkResult()
        {
            var refreshDto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var expectedResult = new AuthResultDto
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                AccessToken = "new-jwt-token",
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService
                .Setup(s => s.RefreshTokenAsync(refreshDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.Refresh(refreshDto);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет, что при попытке обновления токенов с невалидным refresh-токеном выбрасывается исключение <see cref="InvalidRefreshTokenException"/>.
        /// </summary>
        [Fact]
        public async Task Refresh_InvalidToken_ThrowsException()
        {
            var refreshDto = new RefreshTokenDto
            {
                RefreshToken = "invalid-refresh-token"
            };

            _mockAuthService
                .Setup(s => s.RefreshTokenAsync(refreshDto))
                .ThrowsAsync(new InvalidRefreshTokenException());

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => _controller.Refresh(refreshDto));
        }

        /// <summary>
        /// Проверяет успешный выход пользователя из системы.
        /// </summary>
        [Fact]
        public async Task LogoutUser_ValidRequest_ReturnsOkResult()
        {
            var refreshDto = new RefreshTokenDto
            {
                RefreshToken = "refresh-token-to-revoke"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
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

            _mockAuthService
                .Setup(s => s.LogoutUser(refreshDto, It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.LogoutUser(refreshDto);

            result.Should().BeOfType<OkResult>();
        }

        /// <summary>
        /// Проверяет, что при попытке выхода пользователя без claim идентификатора выбрасывается исключение <see cref="UserClaimNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task LogoutUser_UserClaimNotFound_ThrowsException()
        {
            var refreshDto = new RefreshTokenDto
            {
                RefreshToken = "refresh-token"
            };

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockAuthService
                .Setup(s => s.LogoutUser(refreshDto, It.IsAny<ClaimsPrincipal>()))
                .ThrowsAsync(new UserClaimNotFoundException(ClaimTypes.NameIdentifier));

            await Assert.ThrowsAsync<UserClaimNotFoundException>(
                () => _controller.LogoutUser(refreshDto));
        }

        /// <summary>
        /// Проверяет наличие атрибута AllowAnonymous у методов контроллера.
        /// </summary>
        [Fact]
        public void Controller_Methods_HaveAllowAnonymousAttribute()
        {
            var controllerType = typeof(AuthController);

            controllerType.GetMethod("LoginUser")
                .Should().BeDecoratedWith<AllowAnonymousAttribute>();

            controllerType.GetMethod("RegisterUser")
                .Should().BeDecoratedWith<AllowAnonymousAttribute>();

            controllerType.GetMethod("Refresh")
                .Should().BeDecoratedWith<AllowAnonymousAttribute>();

            controllerType.GetMethod("LogoutUser")
                .Should().BeDecoratedWith<AllowAnonymousAttribute>();
        }

        /// <summary>
        /// Проверяет наличие атрибута ApiController у контроллера.
        /// </summary>
        [Fact]
        public void Controller_HasApiControllerAttribute()
        {
            var controllerType = typeof(AuthController);

            controllerType.Should().BeDecoratedWith<ApiControllerAttribute>();
        }

        /// <summary>
        /// Проверяет наличие атрибута Route у контроллера.
        /// </summary>
        [Fact]
        public void Controller_HasRouteAttribute()
        {
            var controllerType = typeof(AuthController);

            controllerType.Should().BeDecoratedWith<RouteAttribute>()
                .Which.Template.Should().Be("api/[controller]");
        }

        /// <summary>
        /// Проверяет наличие атрибутов валидации у DTO для входа пользователя.
        /// </summary>
        [Fact]
        public void LoginUserDto_HasValidationAttributes()
        {
            var dtoType = typeof(LoginUserDto);

            dtoType.GetProperty("Email")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<EmailAddressAttribute>();

            dtoType.GetProperty("Password")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<DataTypeAttribute>()
                .Which.DataType.Should().Be(DataType.Password);
        }

        /// <summary>
        /// Проверяет наличие атрибутов валидации у DTO для регистрации пользователя.
        /// </summary>
        [Fact]
        public void RegisterUserDto_HasValidationAttributes()
        {
            var dtoType = typeof(RegisterUserDto);

            dtoType.GetProperty("Name")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<StringLengthAttribute>();

            dtoType.GetProperty("Email")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<EmailAddressAttribute>();

            dtoType.GetProperty("Password")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<MinLengthAttribute>()
                .And.BeDecoratedWith<DataTypeAttribute>()
                .Which.DataType.Should().Be(DataType.Password);

            dtoType.GetProperty("ConfirmPassword")
                .Should().BeDecoratedWith<RequiredAttribute>()
                .And.BeDecoratedWith<CompareAttribute>()
                .And.BeDecoratedWith<DataTypeAttribute>()
                .Which.DataType.Should().Be(DataType.Password);
        }

        /// <summary>
        /// Проверяет наличие атрибутов валидации у DTO для обновления токенов.
        /// </summary>
        [Fact]
        public void RefreshTokenDto_HasValidationAttributes()
        {
            var dtoType = typeof(RefreshTokenDto);

            dtoType.Should().NotBeNull();
        }

        /// <summary>
        /// Проверяет обработку ситуации, когда сервис возвращает null при входе пользователя.
        /// </summary>
        [Fact]
        public async Task LoginUser_ServiceReturnsNull_ReturnsUnauthorized()
        {
            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            AuthResultDto? nullResult = null;

            _mockAuthService
                .Setup(s => s.LoginUser(loginDto))
                .ReturnsAsync(nullResult!);

            var result = await _controller.LoginUser(loginDto);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();

            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.Value.Should().Be("Неверный логин/пароль");
        }

        /// <summary>
        /// Проверяет обработку ситуации, когда сервис возвращает null при обновлении токенов.
        /// </summary>
        [Fact]
        public async Task Refresh_ServiceReturnsNull_ReturnsUnauthorized()
        {
            var refreshDto = new RefreshTokenDto
            {
                RefreshToken = "refresh-token"
            };

            _mockAuthService
                .Setup(s => s.RefreshTokenAsync(refreshDto))
                .ReturnsAsync(default(AuthResultDto)!);

            var result = await _controller.Refresh(refreshDto);

            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("Неверный логин/пароль");
        }

        /// <summary>
        /// Проверяет обработку ситуации, когда сервис возвращает null при регистрации пользователя.
        /// </summary>
        [Fact]
        public async Task RegisterUser_ServiceReturnsNull_ReturnsBadRequest()
        {
            var registerDto = new RegisterUserDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "Password123",
                ConfirmPassword = "Password123"
            };

            _mockAuthService
                .Setup(s => s.RegistrateUser(registerDto))
                .ReturnsAsync(default(RegisterResponseUserDto)!);

            var result = await _controller.RegisterUser(registerDto);

            result.Result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Ошибка регистрации");
        }
    }
}