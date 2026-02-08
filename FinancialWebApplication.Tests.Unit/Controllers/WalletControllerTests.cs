using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using WebApplication3.Controllers;
using WebApplication3.Services;
using WebApplication3.DTOs.Filters;
using WebApplication3.Models.Common;
using WebApplication3.DTOs.Transaction;
using WebApplication3.DTOs.Wallet;

namespace WebApplication3.Tests.Controllers
{
    /// <summary>
    /// Тесты для <see cref="WalletController"/>.
    /// </summary>
    public class WalletControllerTests
    {
        /// <summary>
        /// Мок сервиса кошельков.
        /// </summary>
        private readonly Mock<IWalletService> _mockWalletService;

        /// <summary>
        /// Мок логгера контроллера кошельков.
        /// </summary>
        private readonly Mock<ILogger<WalletController>> _mockLogger;

        /// <summary>
        /// Мок кэша в памяти.
        /// </summary>
        private readonly Mock<IMemoryCache> _mockCache;

        /// <summary>
        /// Экземпляр тестируемого контроллера кошельков.
        /// </summary>
        private readonly WalletController _controller;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="WalletControllerTests"/>.
        /// </summary>
        public WalletControllerTests()
        {
            _mockWalletService = new Mock<IWalletService>();
            _mockLogger = new Mock<ILogger<WalletController>>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new WalletController(_mockWalletService.Object, _mockLogger.Object, _mockCache.Object);
        }

        /// <summary>
        /// Проверяет успешное создание кошелька с валидными данными.
        /// </summary>
        [Fact]
        public async Task CreateWallet_ValidRequest_ReturnsCreatedResult()
        {
            var userId = 1;
            var createDto = new CreateWalletInputDto
            {
                Name = "Основной кошелёк",
                Currency = "RUB",
                InitialBalance = 1000
            };

            var expectedResult = new CreatedWalletOutputDto
            {
                Id = 1,
                Name = "Основной кошелёк",
                Currency = "RUB",
                InitialBalance = 1000,
                CurrentBalance = 1000
            };

            _mockWalletService
                .Setup(s => s.CreateWallet(userId, createDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.CreateWallet(userId, createDto);

            result.Should().BeOfType<CreatedResult>();
            var createdResult = result as CreatedResult;
            createdResult!.Location.Should().Be($"api/wallet/{expectedResult.Id}");
            createdResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет успешное получение существующего кошелька.
        /// </summary>
        [Fact]
        public async Task GetWallet_ExistingWallet_ReturnsOkResult()
        {
            var walletId = 1;
            var expectedWallet = new WalletDto
            {
                Name = "Основной",
                Currency = "RUB",
                CurrentBalance = 1500
            };

            _mockWalletService
                .Setup(s => s.GetWallet(walletId))
                .ReturnsAsync(expectedWallet);

            var result = await _controller.GetWallet(walletId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedWallet);
        }

        /// <summary>
        /// Проверяет успешное получение всех кошельков пользователя с валидными параметрами.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_ValidParameters_ReturnsOkResult()
        {
            var userId = 1;
            var pageNumber = 1;
            var pageSize = 10;
            var filter = new WalletFilter();

            var wallets = new List<WalletDto>
            {
                new WalletDto { Name = "Кошелёк 1", Currency = "RUB", CurrentBalance = 1000 },
                new WalletDto { Name = "Кошелёк 2", Currency = "USD", CurrentBalance = 500 }
            };

            var pagedResponse = new PagedResponse<WalletDto>(wallets, pageNumber, pageSize, 2);

            _mockWalletService
                .Setup(s => s.GetAllUserWallets(userId, filter, pageNumber, pageSize))
                .ReturnsAsync(pagedResponse);

            var result = await _controller.GetAllUserWallets(userId, filter, pageNumber, pageSize);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(pagedResponse);
        }

        /// <summary>
        /// Проверяет, что при невалидной пагинации возвращается ошибка BadRequest.
        /// </summary>
        [Theory]
        [InlineData(0, 10, "Номер страницы должен быть больше 0")]
        [InlineData(1, 0, "Размер страницы должен быть от 1 до 100")]
        [InlineData(1, 101, "Размер страницы должен быть от 1 до 100")]
        public async Task GetAllUserWallets_InvalidPagination_ReturnsBadRequest(
            int pageNumber, int pageSize, string expectedError)
        {
            var userId = 1;

            var result = await _controller.GetAllUserWallets(userId, null, pageNumber, pageSize);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be(expectedError);
        }

        /// <summary>
        /// Проверяет, что при невалидном фильтре возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_InvalidFilter_ReturnsBadRequest()
        {
            var userId = 1;
            var filter = new WalletFilter
            {
                CurrentBalance = -100,
                CurrencyEquals = "TOOLONGCURRENCYCODE",
                NameEquals = new string('a', 101)
            };

            var result = await _controller.GetAllUserWallets(userId, filter, 1, 10);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Проверяет валидацию поля текущего баланса в фильтре.
        /// </summary>
        [Theory]
        [InlineData(-100, "Текущий баланс не может быть меньше 0.")]
        [InlineData(100, null)]
        public async Task GetAllUserWallets_CurrentBalanceValidation_ReturnsCorrectResult(
            int? currentBalance, string expectedError)
        {
            var userId = 1;
            var filter = new WalletFilter { CurrentBalance = currentBalance };

            if (expectedError == null)
            {
                var pagedResponse = new PagedResponse<WalletDto>(new List<WalletDto>(), 1, 10, 0);
                _mockWalletService
                    .Setup(s => s.GetAllUserWallets(userId, filter, 1, 10))
                    .ReturnsAsync(pagedResponse);
            }

            var result = await _controller.GetAllUserWallets(userId, filter, 1, 10);

            if (expectedError != null)
            {
                result.Should().BeOfType<BadRequestObjectResult>();
                (result as BadRequestObjectResult)!.Value.Should().Be(expectedError);
            }
            else
            {
                result.Should().BeOfType<OkObjectResult>();
            }
        }

        /// <summary>
        /// Проверяет успешное обновление существующего кошелька.
        /// </summary>
        [Fact]
        public async Task UpdateWallet_ValidRequest_ReturnsOkResult()
        {
            var walletId = 1;
            var updateDto = new UpdateWalletDto
            {
                Name = "Обновлённый кошелёк",
                Currency = "USD"
            };

            var expectedResult = new UpdateWalletDto
            {
                Name = "Обновлённый кошелёк",
                Currency = "USD"
            };

            _mockWalletService
                .Setup(s => s.UpdateWallet(walletId, updateDto))
                .ReturnsAsync(expectedResult);

            var result = await _controller.UpdateWallet(walletId, updateDto);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет успешное удаление существующего кошелька.
        /// </summary>
        [Fact]
        public async Task DeleteWallet_ExistingWallet_ReturnsNoContent()
        {
            var walletId = 1;

            var result = await _controller.Delete(walletId);

            result.Should().BeOfType<NoContentResult>();
            _mockWalletService.Verify(s => s.DeleteWallet(walletId), Times.Once);
        }

        /// <summary>
        /// Проверяет успешное получение месячной сводки по кошельку без кэшированных данных.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_ValidRequest_ReturnsOkResult()
        {
            var walletId = 1;
            var year = 2024;
            var month = 1;
            var cacheKey = $"monthlySummary_{walletId}_{month}_{year}";

            var expectedSummary = new WalletMonthlySummaryDto
            {
                Id = walletId,
                Name = "Основной",
                Currency = "RUB",
                Income = 5000,
                Expense = 3000,
                Year = year,
                Month = month
            };

            object? cachedValue = null;

            _mockCache
                .Setup(c => c.TryGetValue(cacheKey, out cachedValue))
                .Returns(false);

            _mockCache
                .Setup(c => c.CreateEntry(cacheKey))
                .Returns(Mock.Of<ICacheEntry>);

            _mockWalletService
                .Setup(s => s.GetWalletMonthlySummary(walletId, year, month))
                .ReturnsAsync(expectedSummary);

            var result = await _controller.GetWalletMonthlySummary(walletId, year, month);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedSummary);
        }

        /// <summary>
        /// Проверяет получение месячной сводки по кошельку из кэша.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_CachedData_ReturnsCachedResult()
        {
            var walletId = 1;
            var year = 2024;
            var month = 1;
            var cacheKey = $"monthlySummary_{walletId}_{month}_{year}";

            var cachedSummary = new WalletMonthlySummaryDto
            {
                Id = walletId,
                Name = "Основной",
                Currency = "RUB",
                Income = 5000,
                Expense = 3000,
                Year = year,
                Month = month
            };

            object cachedValue = cachedSummary;
            _mockCache.Setup(c => c.TryGetValue(cacheKey, out cachedValue)).Returns(true);

            var result = await _controller.GetWalletMonthlySummary(walletId, year, month);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(cachedSummary);

            _mockWalletService.Verify(s => s.GetWalletMonthlySummary(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Проверяет успешное получение топ-3 расходов по кошелькам без кэшированных данных.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensePerWallet_ValidRequest_ReturnsOkResult()
        {
            var userId = 1;
            var year = 2024;
            var month = 1;

            var expectedResult = new List<WalletTopExpensesDto>
            {
                new WalletTopExpensesDto
                {
                    Id = 1,
                    Name = "Основной",
                    Currency = "RUB",
                    TopThreeExpenses = new List<TransactionDto>
                    {
                        new TransactionDto { Id = 1, Sum = 500, Description = "Продукты" },
                        new TransactionDto { Id = 2, Sum = 300, Description = "Транспорт" }
                    }
                }
            };

            var mockCacheEntry = new Mock<ICacheEntry>();
            mockCacheEntry.SetupAllProperties();

            object cachedValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
                      .Returns(false);

            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                      .Returns(mockCacheEntry.Object);

            _mockWalletService
                .Setup(s => s.GetTopThreeExpensePerWallet(userId, year, month))
                .ReturnsAsync(expectedResult);

            var result = await _controller.GetTopThreeExpensePerWallet(userId, year, month);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResult);
        }

        /// <summary>
        /// Проверяет получение топ-3 расходов по кошелькам из кэша.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensePerWallet_CachedData_ReturnsCachedResult()
        {
            var userId = 1;
            var year = 2024;
            var month = 1;
            var cacheKey = $"topThreeExpensesPerWallet_{userId}_{month}_{year}";

            var cachedResult = new List<WalletTopExpensesDto>
            {
                new WalletTopExpensesDto
                {
                    Id = 1,
                    Name = "Основной",
                    Currency = "RUB",
                    TopThreeExpenses = new List<TransactionDto>()
                }
            };

            object cachedValue = cachedResult;
            _mockCache.Setup(c => c.TryGetValue(cacheKey, out cachedValue)).Returns(true);

            var result = await _controller.GetTopThreeExpensePerWallet(userId, year, month);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(cachedResult);

            _mockWalletService.Verify(s => s.GetTopThreeExpensePerWallet(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Проверяет, что при невалидной модели для обновления кошелька возвращается ошибка BadRequest.
        /// </summary>
        [Fact]
        public async Task UpdateWallet_InvalidModel_ReturnsBadRequest()
        {
            var walletId = 1;
            var invalidDto = new UpdateWalletDto
            {
                Name = "",
                Currency = "R"
            };

            _controller.ModelState.Clear();

            _controller.ModelState.AddModelError("Name", "Название кошелька обязательно.");
            _controller.ModelState.AddModelError("Currency", "Код валюты должен быть 3 символа");

            _controller.ModelState.IsValid.Should().BeFalse();
            _controller.ModelState.ErrorCount.Should().Be(2);

            _mockWalletService
                .Setup(s => s.UpdateWallet(It.IsAny<int>(), It.IsAny<UpdateWalletDto>()))
                .Verifiable();

            var result = await _controller.UpdateWallet(walletId, invalidDto);

            result.GetType().Name.Should().Be("BadRequestObjectResult");

            result.Should().BeOfType<BadRequestObjectResult>();

            _mockWalletService.Verify(
                s => s.UpdateWallet(It.IsAny<int>(), It.IsAny<UpdateWalletDto>()),
                Times.Never);
        }

        /// <summary>
        /// Проверяет получение всех кошельков пользователя с null фильтром.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_NullFilter_DoesNotThrow()
        {
            var userId = 1;
            var pagedResponse = new PagedResponse<WalletDto>(new List<WalletDto>(), 1, 10, 0);

            _mockWalletService
                .Setup(s => s.GetAllUserWallets(userId, null, 1, 10))
                .ReturnsAsync(pagedResponse);

            var result = await _controller.GetAllUserWallets(userId, null, 1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }

        /// <summary>
        /// Проверяет получение всех кошельков пользователя с фильтром, содержащим пустые строки.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_FilterWithEmptyStrings_DoesNotValidateLength()
        {
            var userId = 1;
            var filter = new WalletFilter
            {
                CurrencyEquals = "",
                NameEquals = ""
            };

            var pagedResponse = new PagedResponse<WalletDto>(new List<WalletDto>(), 1, 10, 0);

            _mockWalletService
                .Setup(s => s.GetAllUserWallets(userId, filter, 1, 10))
                .ReturnsAsync(pagedResponse);

            var result = await _controller.GetAllUserWallets(userId, filter, 1, 10);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}