using Moq;
using Microsoft.Extensions.Logging;
using WebApplication3.Services;
using WebApplication3.Repositories;
using WebApplication3.Models;
using WebApplication3.DTOs.Filters;
using FluentAssertions;
using WebApplication3.DTOs.Wallet;

namespace FinancialWebApplication.Tests.Unit.Services
{
    /// <summary>
    /// Тесты для <see cref="WalletService"/>.
    /// </summary>
    public class WalletServiceTests
    {
        /// <summary>
        /// Мок репозитория кошельков.
        /// </summary>
        private readonly Mock<IWalletRepository> _mockWalletRepository;

        /// <summary>
        /// Мок логгера сервиса кошельков.
        /// </summary>
        private readonly Mock<ILogger<WalletService>> _mockLogger;

        /// <summary>
        /// Экземпляр тестируемого сервиса кошельков.
        /// </summary>
        private readonly WalletService _walletService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="WalletServiceTests"/>.
        /// </summary>
        public WalletServiceTests()
        {
            _mockWalletRepository = new Mock<IWalletRepository>();
            _mockLogger = new Mock<ILogger<WalletService>>();

            _walletService = new WalletService(
                _mockWalletRepository.Object,
                _mockLogger.Object
            );
        }

        /// <summary>
        /// Проверяет успешное получение существующего кошелька с текущим балансом.
        /// </summary>
        [Fact]
        public async Task GetWallet_ExistingWallet_ReturnsWalletDto()
        {
            var walletId = 1;
            var walletFromRepo = new Wallet
            {
                Id = walletId,
                Name = "Основной кошелёк",
                Currency = "RUB",
                InitialBalance = 1000
            };

            var currentBalance = 1500; 

            _mockWalletRepository
                .Setup(r => r.GetWallet(walletId))
                .ReturnsAsync(walletFromRepo);

            _mockWalletRepository
                .Setup(r => r.GetWalletCurrentBalance(walletId))
                .ReturnsAsync(currentBalance); 

            var result = await _walletService.GetWallet(walletId);

            result.Should().NotBeNull();
            result.Name.Should().Be("Основной кошелёк");
            result.Currency.Should().Be("RUB");
            result.CurrentBalance.Should().Be(1500);

            _mockWalletRepository.Verify(r => r.GetWallet(walletId), Times.Once);
            _mockWalletRepository.Verify(r => r.GetWalletCurrentBalance(walletId), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при попытке получить несуществующий кошелёк выбрасывается исключение.
        /// </summary>
        [Fact]
        public async Task GetWallet_NonExistingWallet_ThrowsException()
        {
            var walletId = 999;

            _mockWalletRepository
                .Setup(r => r.GetWallet(walletId))
                .ThrowsAsync(new Exception("Wallet not found"));

            await Assert.ThrowsAsync<Exception>(() =>
                _walletService.GetWallet(walletId));
        }

        /// <summary>
        /// Проверяет успешное создание нового кошелька.
        /// </summary>
        [Fact]
        public async Task CreateWallet_ValidData_CreatesAndReturnsWallet()
        {
            var userId = 1;
            var createDto = new CreateWalletInputDto
            {
                Name = "Новый кошелёк",
                Currency = "USD",
                InitialBalance = 500
            };

            var createdWallet = new Wallet
            {
                Id = 1,
                Name = createDto.Name,
                Currency = createDto.Currency,
                InitialBalance = createDto.InitialBalance,
                UserId = userId,
            };

            _mockWalletRepository
                .Setup(r => r.AddWallet(It.Is<Wallet>(w =>
                    w.Name == createDto.Name &&
                    w.Currency == createDto.Currency &&
                    w.InitialBalance == createDto.InitialBalance &&
                    w.UserId == userId)))
                .ReturnsAsync(createdWallet);

            var result = await _walletService.CreateWallet(userId, createDto);

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Новый кошелёк");
            result.Currency.Should().Be("USD");
            result.InitialBalance.Should().Be(500);

            _mockWalletRepository.Verify(r => r.AddWallet(It.IsAny<Wallet>()), Times.Once);
        }

        /// <summary>
        /// Проверяет успешное обновление существующего кошелька.
        /// </summary>
        [Fact]
        public async Task UpdateWallet_ValidData_UpdatesAndReturnsWallet()
        {
            var walletId = 1;
            var updateDto = new UpdateWalletDto
            {
                Name = "Обновлённое название",
                Currency = "EUR"
            };

            var existingWallet = new Wallet
            {
                Id = walletId,
                Name = "Старое название",
                Currency = "RUB"
            };

            _mockWalletRepository
                .Setup(r => r.GetWallet(walletId))
                .ReturnsAsync(existingWallet);

            _mockWalletRepository
                .Setup(r => r.UpdateWallet(It.Is<Wallet>(w =>
                    w.Id == walletId &&
                    w.Name == updateDto.Name &&
                    w.Currency == updateDto.Currency)))
                .Returns(Task.CompletedTask);

            var result = await _walletService.UpdateWallet(walletId, updateDto);

            result.Should().NotBeNull();
            result.Name.Should().Be("Обновлённое название");
            result.Currency.Should().Be("EUR");

            existingWallet.Name.Should().Be("Обновлённое название");
            existingWallet.Currency.Should().Be("EUR");

            _mockWalletRepository.Verify(r => r.GetWallet(walletId), Times.Once);
            _mockWalletRepository.Verify(r => r.UpdateWallet(It.IsAny<Wallet>()), Times.Once);
        }

        /// <summary>
        /// Проверяет успешное удаление кошелька.
        /// </summary>
        [Fact]
        public async Task DeleteWallet_ValidId_DeletesWallet()
        {
            var walletId = 1;

            _mockWalletRepository
                .Setup(r => r.DeleteWallet(walletId))
                .Returns(Task.CompletedTask);

            await _walletService.DeleteWallet(walletId);

            _mockWalletRepository.Verify(r => r.DeleteWallet(walletId), Times.Once);
        }

        /// <summary>
        /// Проверяет получение всех кошельков пользователя без фильтра с пагинацией и балансами.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_NoFilter_ReturnsPagedWallets()
        {
            // Arrange
            var userId = 1;
            var pageNumber = 1;
            var pageSize = 10;

            var walletsFromRepo = new List<Wallet>
            {
                new Wallet { Id = 1, Name = "Кошелёк 1", Currency = "RUB", InitialBalance = 1000 },
                new Wallet { Id = 2, Name = "Кошелёк 2", Currency = "USD", InitialBalance = 500 }
            };

            var totalCount = 2;

            var balances = new Dictionary<int, int>
            {
                { 1, 1500 },
                { 2, 700 }   
            };

            _mockWalletRepository
                .Setup(r => r.GetAllUserWallets(userId, null, pageNumber, pageSize))
                .ReturnsAsync((walletsFromRepo, totalCount));

            _mockWalletRepository
                .Setup(r => r.GetAllWalletsCurrentBalance(userId))
                .ReturnsAsync(balances); 

            var result = await _walletService.GetAllUserWallets(userId, null, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2); 
            result.PageNumber.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);
            result.TotalRecords.Should().Be(totalCount);

            result.Data[0].Name.Should().Be("Кошелёк 1");
            result.Data[0].Currency.Should().Be("RUB");
            result.Data[0].CurrentBalance.Should().Be(1500);

            result.Data[1].Name.Should().Be("Кошелёк 2");
            result.Data[1].Currency.Should().Be("USD");
            result.Data[1].CurrentBalance.Should().Be(700); 

            _mockWalletRepository.Verify(r =>
                r.GetAllUserWallets(userId, null, pageNumber, pageSize),
                Times.Once);

            _mockWalletRepository.Verify(r =>
                r.GetAllWalletsCurrentBalance(userId),
                Times.Once);
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя с применением фильтра.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WithFilter_ReturnsFilteredWallets()
        {
            // Arrange
            var userId = 1;
            var filter = new WalletFilter { NameEquals = "Основной" };
            var pageNumber = 1;
            var pageSize = 10;

            var walletsFromRepo = new List<Wallet>
            {
                new Wallet { Id = 1, Name = "Основной кошелёк", Currency = "RUB", InitialBalance = 1000 }
            };

            var totalCount = 1;
            var balances = new Dictionary<int, int> { { 1, 1500 } }; 

            _mockWalletRepository
                .Setup(r => r.GetAllUserWallets(userId, filter, pageNumber, pageSize))
                .ReturnsAsync((walletsFromRepo, totalCount));

            _mockWalletRepository
                .Setup(r => r.GetAllWalletsCurrentBalance(userId))
                .ReturnsAsync(balances);

            var result = await _walletService.GetAllUserWallets(userId, filter, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1); 
            result.Data[0].Name.Should().Be("Основной кошелёк");
            result.TotalRecords.Should().Be(1);

            _mockWalletRepository.Verify(r =>
                r.GetAllUserWallets(userId, filter, pageNumber, pageSize),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что для кошельков без текущего баланса используется начальный баланс.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WalletWithoutBalance_UsesInitialBalance()
        {
            var userId = 1;
            var walletsFromRepo = new List<Wallet>
            {
                new Wallet { Id = 1, Name = "Кошелёк", Currency = "RUB", InitialBalance = 1000 }
            };

            var totalCount = 1;
            var balances = new Dictionary<int, int>(); 

            _mockWalletRepository
                .Setup(r => r.GetAllUserWallets(userId, null, 1, 10))
                .ReturnsAsync((walletsFromRepo, totalCount));

            _mockWalletRepository
                .Setup(r => r.GetAllWalletsCurrentBalance(userId))
                .ReturnsAsync(balances);

            var result = await _walletService.GetAllUserWallets(userId);

            result.Should().NotBeNull();
            result.Data[0].CurrentBalance.Should().Be(1000);
        }

        /// <summary>
        /// Проверяет успешное получение месячной сводки по кошельку.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_ValidData_ReturnsSummary()
        {
            // Arrange
            var walletId = 1;
            var year = 2024;
            var month = 3;

            var repositoryResult = ("Основной кошелёк", "RUB", 50000, 35000);

            _mockWalletRepository
                .Setup(r => r.GetWalletMonthlySummary(walletId, year, month))
                .ReturnsAsync(repositoryResult); 

            var result = await _walletService.GetWalletMonthlySummary(walletId, year, month);

            result.Should().NotBeNull();
            result.Id.Should().Be(walletId);
            result.Name.Should().Be("Основной кошелёк");
            result.Currency.Should().Be("RUB");
            result.Income.Should().Be(50000);
            result.Expense.Should().Be(35000);
            result.Year.Should().Be(year);
            result.Month.Should().Be(month);

            _mockWalletRepository.Verify(r =>
                r.GetWalletMonthlySummary(walletId, year, month),
                Times.Once);
        }

        /// <summary>
        /// Проверяет успешное получение топ-3 расходов для каждого кошелька пользователя.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensePerWallet_ValidData_ReturnsTopExpenses()
        {
            // Arrange
            var userId = 1;
            var year = 2024;
            var month = 3;

            var topExpenses = new Dictionary<int, List<Transaction>>
            {
                { 1, new List<Transaction>
                    {
                        new Transaction { Id = 1, TransactionSum = 5000, Description = "Еда", Type = TransactionType.Expense },
                        new Transaction { Id = 2, TransactionSum = 3000, Description = "Транспорт", Type = TransactionType.Expense }
                    }
                }
            };

            var wallets = new List<Wallet>
            {
                new Wallet { Id = 1, Name = "Кошелёк 1", Currency = "RUB" },
                new Wallet { Id = 2, Name = "Кошелёк 2", Currency = "USD" }
            };

            _mockWalletRepository
                .Setup(r => r.GetTopThreeExpensesPerWallet(userId, year, month))
                .ReturnsAsync(topExpenses);

            _mockWalletRepository
                .Setup(r => r.GetAllUserWallets(userId, null, 1, int.MaxValue))
                .ReturnsAsync((wallets, wallets.Count));

            var result = await _walletService.GetTopThreeExpensePerWallet(userId, year, month);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            var wallet1 = result.First(w => w.Id == 1);
            wallet1.Name.Should().Be("Кошелёк 1");
            wallet1.Currency.Should().Be("RUB");
            wallet1.TopThreeExpenses.Should().HaveCount(2);
            wallet1.TopThreeExpenses[0].Sum.Should().Be(5000);
            wallet1.TopThreeExpenses[0].Description.Should().Be("Еда");

            var wallet2 = result.First(w => w.Id == 2);
            wallet2.Name.Should().Be("Кошелёк 2");
            wallet2.Currency.Should().Be("USD");
            wallet2.TopThreeExpenses.Should().BeEmpty();

            _mockWalletRepository.Verify(r =>
                r.GetTopThreeExpensesPerWallet(userId, year, month),
                Times.Once);

            _mockWalletRepository.Verify(r =>
                r.GetAllUserWallets(userId, null, 1, int.MaxValue),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что при отсутствии расходов возвращаются кошельки с пустыми списками расходов.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensePerWallet_NoExpenses_ReturnsEmptyLists()
        {
            var userId = 1;
            var year = 2024;
            var month = 3;

            var topExpenses = new Dictionary<int, List<Transaction>>(); 
            var wallets = new List<Wallet>
            {
                new Wallet { Id = 1, Name = "Кошелёк 1", Currency = "RUB" }
            };

            _mockWalletRepository
                .Setup(r => r.GetTopThreeExpensesPerWallet(userId, year, month))
                .ReturnsAsync(topExpenses);

            _mockWalletRepository
                .Setup(r => r.GetAllUserWallets(userId, null, 1, int.MaxValue))
                .ReturnsAsync((wallets, wallets.Count));

            var result = await _walletService.GetTopThreeExpensePerWallet(userId, year, month);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].TopThreeExpenses.Should().BeEmpty();
        }
    }
}