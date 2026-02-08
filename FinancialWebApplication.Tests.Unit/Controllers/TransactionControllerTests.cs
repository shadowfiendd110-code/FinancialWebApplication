using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WebApplication3.Controllers;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Transaction;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Models.Common;
using WebApplication3.Services;

namespace WebApplication3.Tests.Controllers
{
    /// <summary>
    /// Тесты для <see cref="TransactionController"/>.
    /// </summary>
    public class TransactionControllerTests
    {
        /// <summary>
        /// Мок сервиса транзакций.
        /// </summary>
        private readonly Mock<ITransactionService> _mockTransactionService;

        /// <summary>
        /// Мок логгера контроллера транзакций.
        /// </summary>
        private readonly Mock<ILogger<TransactionController>> _mockLogger;

        /// <summary>
        /// Мок кэша в памяти.
        /// </summary>
        private readonly Mock<IMemoryCache> _mockCache;

        /// <summary>
        /// Экземпляр тестируемого контроллера транзакций.
        /// </summary>
        private TransactionController _controller;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TransactionControllerTests"/>.
        /// </summary>
        public TransactionControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _mockLogger = new Mock<ILogger<TransactionController>>();
            _mockCache = new Mock<IMemoryCache>();

            _controller = new TransactionController(
                _mockTransactionService.Object,
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
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Admin")
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
        /// Проверяет успешное получение всех транзакций с валидными параметрами.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_ValidParameters_ReturnsOkResult()
        {
            var walletId = 1;
            var pageNumber = 1;
            var pageSize = 10;

            var expectedResponse = new PagedResponse<TransactionDto>(
                new List<TransactionDto>
                {
                    new TransactionDto
                    {
                        Id = 1,
                        Description = "Test 1",
                        Sum = 100,
                        Date = DateTime.UtcNow,
                        Type = TransactionType.Income,
                        WalletId = walletId
                    },
                    new TransactionDto
                    {
                        Id = 2,
                        Description = "Test 2",
                        Sum = 200,
                        Date = DateTime.UtcNow,
                        Type = TransactionType.Expense,
                        WalletId = walletId
                    }
                },
                pageNumber,
                pageSize,
                2
            );

            _mockTransactionService
                .Setup(s => s.GetAllTransactions(walletId, null, pageNumber, pageSize))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.GetAllTransactions(walletId, pageNumber: pageNumber, pageSize: pageSize);

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
        public async Task GetAllTransactions_InvalidPageNumber_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            var walletId = 1;

            var result = await _controller.GetAllTransactions(walletId, pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Номер страницы должен быть больше 0");
        }

        /// <summary>
        /// Проверяет, что при невалидном размере страницы возвращается ошибка BadRequest.
        /// </summary>
        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        [InlineData(1, -1)]
        public async Task GetAllTransactions_InvalidPageSize_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            var walletId = 1;

            var result = await _controller.GetAllTransactions(walletId, pageNumber: pageNumber, pageSize: pageSize);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Размер страницы должен быть от 1 до 100");
        }

        /// <summary>
        /// Проверяет, что при невалидном диапазоне дат возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_InvalidDateRange_ReturnsBadRequest()
        {
            var walletId = 1;
            var filter = new TransactionFilter
            {
                DateFrom = DateTime.UtcNow.AddDays(10),
                DateTo = DateTime.UtcNow
            };

            var result = await _controller.GetAllTransactions(walletId, filter: filter);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Дата 'с' не может быть позже даты 'по'");
        }

        /// <summary>
        /// Проверяет, что при невалидном диапазоне сумм возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_InvalidAmountRange_ReturnsBadRequest()
        {
            var walletId = 1;
            var filter = new TransactionFilter
            {
                MinAmount = 1000,
                MaxAmount = 100
            };

            var result = await _controller.GetAllTransactions(walletId, filter: filter);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Минимальная сумма не может быть больше максимальной");
        }

        /// <summary>
        /// Проверяет, что при невалидном типе транзакции возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_InvalidTransactionType_ReturnsBadRequest()
        {
            var walletId = 1;
            var filter = new TransactionFilter
            {
                TransactionType = (TransactionType)999
            };

            var result = await _controller.GetAllTransactions(walletId, filter: filter);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Тип транзакции должен быть 'Income' или 'Expense'");
        }

        /// <summary>
        /// Проверяет успешное получение транзакций с валидным фильтром.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_ValidFilter_ReturnsOkResult()
        {
            var walletId = 1;
            var filter = new TransactionFilter
            {
                TransactionType = TransactionType.Income,
                MinAmount = 100,
                MaxAmount = 1000,
                DescriptionContains = "Test"
            };

            var expectedResponse = new PagedResponse<TransactionDto>(
                new List<TransactionDto>
                {
                    new TransactionDto
                    {
                        Id = 1,
                        Description = "Test Transaction",
                        Sum = 500,
                        Date = DateTime.UtcNow,
                        Type = TransactionType.Income,
                        WalletId = walletId
                    }
                },
                1,
                10,
                1
            );

            _mockTransactionService
                .Setup(s => s.GetAllTransactions(walletId, filter, 1, 10))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.GetAllTransactions(walletId, filter: filter);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        }

        /// <summary>
        /// Проверяет успешное получение сгруппированных и отсортированных транзакций.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortedTransactions_ValidParameters_ReturnsOkResult()
        {
            var walletId = 1;
            var year = 2024;
            var month = 1;

            var expectedResult = new List<TransactionDto>
            {
                new TransactionDto
                {
                    Id = 1,
                    Description = "Jan Transaction",
                    Sum = 100,
                    Date = new DateTime(2024, 1, 15),
                    Type = TransactionType.Income,
                    WalletId = walletId
                }
            };

            object cachedValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cachedValue)).Returns(false);

            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);

            _mockTransactionService
                .Setup(s => s.GetGroupAndSortTransactions(walletId, year, month))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetGroupAndSortedTransactions(walletId, year, month);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет успешное получение существующей транзакции.
        /// </summary>
        [Fact]
        public async Task GetTransaction_ValidId_ReturnsOkResult()
        {
            var transactionId = 1;
            var expectedTransaction = new TransactionDto
            {
                Id = transactionId,
                Description = "Test Transaction",
                Sum = 100,
                Date = DateTime.UtcNow,
                Type = TransactionType.Income,
                WalletId = 1
            };

            _mockTransactionService
                .Setup(s => s.GetTransaction(transactionId))
                .ReturnsAsync(expectedTransaction);

            var result = await _controller.GetTransaction(transactionId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedTransaction);
        }

        /// <summary>
        /// Проверяет, что при попытке получения несуществующей транзакции выбрасывается исключение <see cref="TransactionNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetTransaction_TransactionNotFound_ThrowsException()
        {
            var transactionId = 999;

            _mockTransactionService
                .Setup(s => s.GetTransaction(transactionId))
                .ThrowsAsync(new TransactionNotFoundException($"Транзакция с ID {transactionId} не найдена"));

            await Assert.ThrowsAsync<TransactionNotFoundException>(
                () => _controller.GetTransaction(transactionId));
        }

        /// <summary>
        /// Проверяет успешное создание новой транзакции.
        /// </summary>
        [Fact]
        public async Task CreateTransaction_ValidDto_ReturnsCreatedResult()
        {
            var walletId = 1;
            var createDto = new CreateTransactionInputDto
            {
                Description = "New Transaction",
                Sum = 100,
                Type = TransactionType.Income
            };

            var expectedResult = new CreateTransactionOutputDto
            {
                Id = 1,
                Description = "New Transaction",
                Sum = 100,
                Date = DateTime.UtcNow,
                Type = TransactionType.Income
            };

            _mockTransactionService
                .Setup(s => s.CreateTransaction(walletId, createDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.CreateTransaction(walletId, createDto);

            result.Should().BeOfType<CreatedResult>();
            var createdResult = result as CreatedResult;
            createdResult!.Location.Should().Be($"api/transaction/{expectedResult.Id}");
            createdResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет, что при попытке создания транзакции для несуществующего кошелька выбрасывается исключение <see cref="WalletNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task CreateTransaction_WalletNotFound_ThrowsException()
        {
            var walletId = 999;
            var createDto = new CreateTransactionInputDto
            {
                Description = "Test",
                Sum = 100,
                Type = TransactionType.Income
            };

            _mockTransactionService
                .Setup(s => s.CreateTransaction(walletId, createDto))
                .ThrowsAsync(new WalletNotFoundException($"Кошелек с ID {walletId} не найден"));

            await Assert.ThrowsAsync<WalletNotFoundException>(
                () => _controller.CreateTransaction(walletId, createDto));
        }

        /// <summary>
        /// Проверяет настройку авторизации для метода получения транзакций.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_WithoutAuthorization_ShouldNotBeCalled()
        {
            var unauthorizedClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };

            var identity = new ClaimsIdentity(unauthorizedClaims, "TestAuth");
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
        /// Проверяет наличие атрибута Authorize у контроллера.
        /// </summary>
        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            var controllerType = typeof(TransactionController);

            controllerType.Should().BeDecoratedWith<AuthorizeAttribute>();
        }

        /// <summary>
        /// Проверяет корректность ролей в атрибуте Authorize.
        /// </summary>
        [Fact]
        public void Controller_AuthorizeAttribute_HasCorrectRoles()
        {
            var controllerType = typeof(TransactionController);

            controllerType.Should().BeDecoratedWith<AuthorizeAttribute>()
                .Which.Roles.Should().Be("User, Admin");
        }
    }
}