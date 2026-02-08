using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using WebApplication3.Repositories;
using WebApplication3.Models;
using WebApplication3.Exceptions;
using WebApplication3.DTOs.Filters;
using WebApplication3.Data;

namespace WebApplication3.Tests.Repositories
{
    /// <summary>
    /// Тесты для <see cref="WalletRepository"/>.
    /// </summary>
    public class WalletRepositoryTests : IDisposable
    {
        /// <summary>
        /// Контекст базы данных для тестирования.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Экземпляр тестируемого репозитория кошельков.
        /// </summary>
        private readonly WalletRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="WalletRepositoryTests"/>.
        /// </summary>
        public WalletRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"WalletTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repository = new WalletRepository(_context);
        }

        /// <summary>
        /// Освобождает ресурсы тестового класса.
        /// </summary>
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        /// <summary>
        /// Проверяет успешное добавление кошелька в базу данных.
        /// </summary>
        [Fact]
        public async Task AddWallet_ValidWallet_AddsToDatabase()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet = new Wallet
            {
                Name = "Основной кошелёк",
                Currency = "RUB",
                InitialBalance = 1000,
                UserId = userId
            };

            var result = await _repository.AddWallet(wallet);

            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("Основной кошелёк");
            result.Currency.Should().Be("RUB");
            result.InitialBalance.Should().Be(1000);

            var dbWallet = await _context.Wallets.FindAsync(result.Id);
            dbWallet.Should().NotBeNull();
        }

        /// <summary>
        /// Проверяет успешное получение существующего кошелька с транзакциями.
        /// </summary>
        [Fact]
        public async Task GetWallet_ExistingWallet_ReturnsWalletWithTransactions()
        {
            var wallet = await CreateTestWalletWithTransactions(1, 1);

            var result = await _repository.GetWallet(wallet.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(wallet.Id);
            result.Name.Should().Be("Test Wallet 1"); 
            result.Transactions.Should().HaveCount(3);
        }

        /// <summary>
        /// Проверяет, что при попытке получить несуществующий кошелёк выбрасывается исключение <see cref="WalletNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetWallet_NonExistingWallet_ThrowsWalletNotFoundException()
        {
            await Assert.ThrowsAsync<WalletNotFoundException>(() =>
                _repository.GetWallet(999));
        }

        /// <summary>
        /// Проверяет получение всех кошельков пользователя без фильтра.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_NoFilter_ReturnsAllUserWallets()
        {
            var userId = 1;
            await CreateTestUser(userId);

            await CreateTestWallet(userId, "Wallet 1", "RUB", 100);
            await CreateTestWallet(userId, "Wallet 2", "USD", 200);
            await CreateTestWallet(2, "Other User Wallet", "EUR", 300); 

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId);

            totalCount.Should().Be(2);
            wallets.Should().HaveCount(2);
            wallets.All(w => w.UserId == userId).Should().BeTrue();
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя с фильтром по названию.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WithNameFilter_ReturnsFilteredWallets()
        {
            var userId = 1;
            await CreateTestUser(userId);

            await CreateTestWallet(userId, "Основной", "RUB", 100);
            await CreateTestWallet(userId, "Резервный", "USD", 200);
            await CreateTestWallet(userId, "Долларовый", "USD", 300);

            var filter = new WalletFilter { NameEquals = "Основной" };

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId, filter);

            totalCount.Should().Be(1);
            wallets.Should().HaveCount(1);
            wallets.First().Name.Should().Be("Основной");
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя с фильтром по валюте.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WithCurrencyFilter_ReturnsFilteredWallets()
        {
            var userId = 1;
            await CreateTestUser(userId);

            await CreateTestWallet(userId, "Wallet 1", "RUB", 100);
            await CreateTestWallet(userId, "Wallet 2", "USD", 200);
            await CreateTestWallet(userId, "Wallet 3", "RUB", 300);

            var filter = new WalletFilter { CurrencyEquals = "RUB" };

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId, filter);

            totalCount.Should().Be(2);
            wallets.Should().HaveCount(2);
            wallets.All(w => w.Currency == "RUB").Should().BeTrue();
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя с комбинированным фильтром по названию и валюте.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WithMultipleFilters_ReturnsFilteredWallets()
        {
            var userId = 1;
            await CreateTestUser(userId);

            await CreateTestWallet(userId, "Основной", "RUB", 100);      
            await CreateTestWallet(userId, "Резервный", "RUB", 200);     
            await CreateTestWallet(userId, "Основной", "USD", 300);     

            var filter = new WalletFilter
            {
                NameEquals = "Основной",    
                CurrencyEquals = "RUB"      
            };


            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId, filter);

            totalCount.Should().Be(1); 
            wallets.Should().HaveCount(1);
            wallets.First().Name.Should().Be("Основной");
            wallets.First().Currency.Should().Be("RUB");
        }

        /// <summary>
        /// Проверяет, что пустые строки в фильтре игнорируются.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_EmptyStringFilter_IgnoresFilter()
        {
            var userId = 1;
            await CreateTestUser(userId);

            await CreateTestWallet(userId, "Wallet 1", "RUB", 100);
            await CreateTestWallet(userId, "Wallet 2", "USD", 200);

            var filter = new WalletFilter
            {
                NameEquals = "", 
                CurrencyEquals = "  " 
            };

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId, filter);

            totalCount.Should().Be(2); 
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_WithPagination_ReturnsCorrectPage()
        {
            var userId = 1;
            await CreateTestUser(userId);

            for (int i = 1; i <= 15; i++)
            {
                await CreateTestWallet(userId, $"Wallet {i}", "RUB", i * 100);
            }

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId, pageNumber: 2, pageSize: 5);

            totalCount.Should().Be(15);
            wallets.Should().HaveCount(5);
            wallets.Should().OnlyContain(w => w.UserId == userId);
        }

        /// <summary>
        /// Проверяет получение кошельков пользователя, когда у пользователя нет кошельков.
        /// </summary>
        [Fact]
        public async Task GetAllUserWallets_NoWallets_ReturnsEmptyList()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var (wallets, totalCount) = await _repository.GetAllUserWallets(userId);

            totalCount.Should().Be(0);
            wallets.Should().BeEmpty();
        }

        /// <summary>
        /// Проверяет успешное обновление существующего кошелька.
        /// </summary>
        [Fact]
        public async Task UpdateWallet_ExistingWallet_UpdatesWallet()
        {
            var wallet = await CreateTestWallet(1, "Old Name", "RUB", 100);
            wallet.Name = "New Name";
            wallet.Currency = "USD";

            await _repository.UpdateWallet(wallet);

            var updatedWallet = await _context.Wallets.FindAsync(wallet.Id);
            updatedWallet.Should().NotBeNull();
            updatedWallet!.Name.Should().Be("New Name");
            updatedWallet.Currency.Should().Be("USD");
        }

        /// <summary>
        /// Проверяет успешное удаление существующего кошелька из базы данных.
        /// </summary>
        [Fact]
        public async Task DeleteWallet_ExistingWallet_DeletesFromDatabase()
        {
            var wallet = await CreateTestWallet(1, "To Delete", "RUB", 100);

            await _repository.DeleteWallet(wallet.Id);

            var dbWallet = await _context.Wallets.FindAsync(wallet.Id);
            dbWallet.Should().BeNull();
        }

        /// <summary>
        /// Проверяет, что при попытке удаления несуществующего кошелька выбрасывается исключение <see cref="WalletNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task DeleteWallet_NonExistingWallet_ThrowsWalletNotFoundException()
        {
            await Assert.ThrowsAsync<WalletNotFoundException>(() =>
                _repository.DeleteWallet(999));
        }

        /// <summary>
        /// Проверяет каскадное удаление транзакций при удалении кошелька.
        /// </summary>
        [Fact]
        public async Task DeleteWallet_WithTransactions_DeletesCascading()
        {
            var wallet = await CreateTestWalletWithTransactions(1, 1);
            var transactionCount = await _context.Transactions
                .Where(t => t.WalletId == wallet.Id)
                .CountAsync();
            transactionCount.Should().BeGreaterThan(0);

            await _repository.DeleteWallet(wallet.Id);

            var dbWallet = await _context.Wallets.FindAsync(wallet.Id);
            dbWallet.Should().BeNull();

            var dbTransactions = await _context.Transactions
                .Where(t => t.WalletId == wallet.Id)
                .ToListAsync();
            dbTransactions.Should().BeEmpty();
        }

        /// <summary>
        /// Проверяет расчет текущего баланса кошелька только с начальным балансом.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_WithOnlyInitialBalance_ReturnsInitialBalance()
        {
            var wallet = await CreateTestWallet(1, "Test", "RUB", 1000);

            var balance = await _repository.GetWalletCurrentBalance(wallet.Id);

            balance.Should().Be(1000);
        }

        /// <summary>
        /// Проверяет расчет текущего баланса кошелька с доходами.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_WithIncomeTransactions_ReturnsCorrectBalance()
        {
            var wallet = await CreateTestWallet(1, "Test", "RUB", 1000);

            await CreateTransaction(wallet.Id, "Income 1", 500, TransactionType.Income);
            await CreateTransaction(wallet.Id, "Income 2", 300, TransactionType.Income);

            var balance = await _repository.GetWalletCurrentBalance(wallet.Id);

            balance.Should().Be(1800);
        }

        /// <summary>
        /// Проверяет расчет текущего баланса кошелька с расходами.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_WithExpenseTransactions_ReturnsCorrectBalance()
        {
            var wallet = await CreateTestWallet(1, "Test", "RUB", 1000);

            await CreateTransaction(wallet.Id, "Expense 1", 200, TransactionType.Expense);
            await CreateTransaction(wallet.Id, "Expense 2", 300, TransactionType.Expense);

            var balance = await _repository.GetWalletCurrentBalance(wallet.Id);

            balance.Should().Be(500); 
        }

        /// <summary>
        /// Проверяет расчет текущего баланса кошелька с доходами и расходами.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_WithMixedTransactions_ReturnsCorrectBalance()
        {
            var wallet = await CreateTestWallet(1, "Test", "RUB", 1000);

            await CreateTransaction(wallet.Id, "Income", 500, TransactionType.Income);
            await CreateTransaction(wallet.Id, "Expense", 300, TransactionType.Expense);

            var balance = await _repository.GetWalletCurrentBalance(wallet.Id);

            balance.Should().Be(1200); 
        }

        /// <summary>
        /// Проверяет расчет текущего баланса кошелька с нулевым балансом.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_ZeroBalance_ReturnsZero()
        {
            var wallet = await CreateTestWallet(1, "Test", "RUB", 0);

            await CreateTransaction(wallet.Id, "Income", 100, TransactionType.Income);
            await CreateTransaction(wallet.Id, "Expense", 100, TransactionType.Expense);

            var balance = await _repository.GetWalletCurrentBalance(wallet.Id);

            balance.Should().Be(0);
        }

        /// <summary>
        /// Проверяет, что при попытке получения баланса несуществующего кошелька выбрасывается исключение <see cref="WalletNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetWalletCurrentBalance_NonExistingWallet_ThrowsWalletNotFoundException()
        {
            await Assert.ThrowsAsync<WalletNotFoundException>(() =>
                _repository.GetWalletCurrentBalance(999));
        }

        /// <summary>
        /// Проверяет получение текущих балансов всех кошельков пользователя.
        /// </summary>
        [Fact]
        public async Task GetAllWalletsCurrentBalance_MultipleWallets_ReturnsAllBalances()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet1 = await CreateTestWallet(userId, "Wallet 1", "RUB", 1000);
            var wallet2 = await CreateTestWallet(userId, "Wallet 2", "USD", 1000);

            await CreateTransaction(wallet1.Id, "Income", 500, TransactionType.Income);
            await CreateTransaction(wallet1.Id, "Expense", 200, TransactionType.Expense);

            await CreateTransaction(wallet2.Id, "Income", 1000, TransactionType.Income);
            await CreateTransaction(wallet2.Id, "Expense", 300, TransactionType.Expense);

            var balances = await _repository.GetAllWalletsCurrentBalance(userId);

            balances.Should().HaveCount(2);
            balances[wallet1.Id].Should().Be(1300); 
            balances[wallet2.Id].Should().Be(1700);
        }

        /// <summary>
        /// Проверяет получение текущих балансов для пользователя без кошельков.
        /// </summary>
        [Fact]
        public async Task GetAllWalletsCurrentBalance_NoWallets_ReturnsEmptyDictionary()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var balances = await _repository.GetAllWalletsCurrentBalance(userId);

            balances.Should().NotBeNull();
            balances.Should().BeEmpty();
        }

        /// <summary>
        /// Проверяет получение начальных балансов для кошельков пользователя без транзакций.
        /// </summary>
        [Fact]
        public async Task GetAllWalletsCurrentBalance_UserHasNoTransactions_ReturnsInitialBalances()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet1 = await CreateTestWallet(userId, "Wallet 1", "RUB", 1000);
            var wallet2 = await CreateTestWallet(userId, "Wallet 2", "USD", 2000);

            var balances = await _repository.GetAllWalletsCurrentBalance(userId);

            balances.Should().HaveCount(2);
            balances[wallet1.Id].Should().Be(1000);
            balances[wallet2.Id].Should().Be(2000);
        }

        /// <summary>
        /// Проверяет получение месячной сводки по кошельку с транзакциями в указанном месяце.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_WithTransactionsInMonth_ReturnsSummary()
        {
            var wallet = await CreateTestWallet(1, "Test Wallet", "RUB", 1000);

            var targetDate = new DateTime(2024, 1, 15);
            await CreateTransaction(wallet.Id, "Jan Income", 500, TransactionType.Income, targetDate);
            await CreateTransaction(wallet.Id, "Jan Expense", 200, TransactionType.Expense, targetDate);

            await CreateTransaction(wallet.Id, "Dec Income", 1000, TransactionType.Income, new DateTime(2023, 12, 15));

            var summary = await _repository.GetWalletMonthlySummary(wallet.Id, 2024, 1);

            summary.Name.Should().Be("Test Wallet");
            summary.Currency.Should().Be("RUB");
            summary.Income.Should().Be(500);
            summary.Expense.Should().Be(200);
        }

        /// <summary>
        /// Проверяет получение месячной сводки по кошельку без транзакций в указанном месяце.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_NoTransactionsInMonth_ReturnsZeroSummary()
        {
            var wallet = await CreateTestWallet(1, "Test Wallet", "RUB", 1000);

            await CreateTransaction(wallet.Id, "Other Month", 500, TransactionType.Income, new DateTime(2024, 2, 15));

            var summary = await _repository.GetWalletMonthlySummary(wallet.Id, 2024, 1);

            summary.Income.Should().Be(0);
            summary.Expense.Should().Be(0);
        }

        /// <summary>
        /// Проверяет, что при попытке получения месячной сводки для несуществующего кошелька выбрасывается исключение <see cref="WalletNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetWalletMonthlySummary_NonExistingWallet_ThrowsWalletNotFoundException()
        {
            await Assert.ThrowsAsync<WalletNotFoundException>(() =>
                _repository.GetWalletMonthlySummary(999, 2024, 1));
        }

        /// <summary>
        /// Проверяет получение топ-3 расходов для кошелька в указанном месяце.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensesPerWallet_WithExpenses_ReturnsTopThree()
        {
            // Arrange
            var userId = 1;
            await CreateTestUser(userId);

            var wallet = await CreateTestWallet(userId, "Test Wallet", "RUB", 1000);

            var targetDate = new DateTime(2024, 1, 15);
            await CreateTransaction(wallet.Id, "Expense 1", 100, TransactionType.Expense, targetDate);
            await CreateTransaction(wallet.Id, "Expense 2", 300, TransactionType.Expense, targetDate);
            await CreateTransaction(wallet.Id, "Expense 3", 200, TransactionType.Expense, targetDate);
            await CreateTransaction(wallet.Id, "Expense 4", 50, TransactionType.Expense, targetDate); 

            await CreateTransaction(wallet.Id, "Income", 500, TransactionType.Income, targetDate);

            await CreateTransaction(wallet.Id, "Other Month", 400, TransactionType.Expense, new DateTime(2024, 2, 15));

            var result = await _repository.GetTopThreeExpensesPerWallet(userId, 2024, 1);

            result.Should().HaveCount(1);
            result.Should().ContainKey(wallet.Id);

            var expenses = result[wallet.Id];
            expenses.Should().HaveCount(3);
            expenses.Select(e => e.TransactionSum).Should().BeInDescendingOrder();
            expenses[0].TransactionSum.Should().Be(300);
            expenses[1].TransactionSum.Should().Be(200);
            expenses[2].TransactionSum.Should().Be(100);
        }

        /// <summary>
        /// Проверяет получение топ-3 расходов для нескольких кошельков пользователя.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensesPerWallet_MultipleWallets_ReturnsForEachWallet()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet1 = await CreateTestWallet(userId, "Wallet 1", "RUB", 1000);
            var wallet2 = await CreateTestWallet(userId, "Wallet 2", "USD", 2000);

            var targetDate = new DateTime(2024, 1, 15);
            await CreateTransaction(wallet1.Id, "Expense", 100, TransactionType.Expense, targetDate);
            await CreateTransaction(wallet2.Id, "Expense", 200, TransactionType.Expense, targetDate);

            var result = await _repository.GetTopThreeExpensesPerWallet(userId, 2024, 1);

            result.Should().HaveCount(2);
            result.Should().ContainKey(wallet1.Id);
            result.Should().ContainKey(wallet2.Id);
        }

        /// <summary>
        /// Проверяет получение топ-3 расходов при отсутствии расходов в указанном месяце.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensesPerWallet_NoExpensesInMonth_ReturnsEmptyDictionary()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet = await CreateTestWallet(userId, "Test Wallet", "RUB", 1000);

            await CreateTransaction(wallet.Id, "Income", 500, TransactionType.Income, new DateTime(2024, 1, 15));

            var result = await _repository.GetTopThreeExpensesPerWallet(userId, 2024, 1);

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Проверяет получение доступных расходов, когда их меньше трех.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensesPerWallet_LessThanThreeExpenses_ReturnsAvailable()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var wallet = await CreateTestWallet(userId, "Test Wallet", "RUB", 1000);

            var targetDate = new DateTime(2024, 1, 15);
            await CreateTransaction(wallet.Id, "Expense 1", 300, TransactionType.Expense, targetDate);
            await CreateTransaction(wallet.Id, "Expense 2", 100, TransactionType.Expense, targetDate);

            var result = await _repository.GetTopThreeExpensesPerWallet(userId, 2024, 1);

            result.Should().HaveCount(1);
            var expenses = result[wallet.Id];
            expenses.Should().HaveCount(2);
        }

        /// <summary>
        /// Проверяет получение топ-3 расходов для пользователя без кошельков.
        /// </summary>
        [Fact]
        public async Task GetTopThreeExpensesPerWallet_UserHasNoWallets_ReturnsEmptyDictionary()
        {
            var userId = 1;
            await CreateTestUser(userId);

            var result = await _repository.GetTopThreeExpensesPerWallet(userId, 2024, 1);

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Создает тестового пользователя в базе данных.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Созданный пользователь.</returns>
        private async Task<User> CreateTestUser(int userId)
        {
            var user = new User
            {
                Id = userId,
                Name = $"User {userId}",
                Email = $"user{userId}@example.com",
                PasswordHash = "hash",
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Создает тестовый кошелек в базе данных.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="name">Название кошелька.</param>
        /// <param name="currency">Валюта кошелька.</param>
        /// <param name="initialBalance">Начальный баланс кошелька.</param>
        /// <returns>Созданный кошелек.</returns>
        private async Task<Wallet> CreateTestWallet(int userId, string name, string currency, int initialBalance)
        {
            var wallet = new Wallet
            {
                Name = name,
                Currency = currency,
                InitialBalance = initialBalance,
                UserId = userId
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
            return wallet;
        }

        /// <summary>
        /// Создает тестовый кошелек с транзакциями в базе данных.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="walletNumber">Номер кошелька (для генерации названия).</param>
        /// <returns>Созданный кошелек с транзакциями.</returns>
        private async Task<Wallet> CreateTestWalletWithTransactions(int userId, int walletNumber)
        {
            var wallet = await CreateTestWallet(userId, $"Test Wallet {walletNumber}", "RUB", 1000);

            await CreateTransaction(wallet.Id, $"Income {walletNumber}-1", 500, TransactionType.Income);
            await CreateTransaction(wallet.Id, $"Expense {walletNumber}-1", 200, TransactionType.Expense);
            await CreateTransaction(wallet.Id, $"Income {walletNumber}-2", 300, TransactionType.Income);

            return wallet;
        }

        /// <summary>
        /// Создает тестовую транзакцию в базе данных.
        /// </summary>
        /// <param name="walletId">Идентификатор кошелька.</param>
        /// <param name="description">Описание транзакции.</param>
        /// <param name="amount">Сумма транзакции.</param>
        /// <param name="type">Тип транзакции.</param>
        /// <param name="date">Дата транзакции (по умолчанию текущая).</param>
        /// <returns>Созданная транзакция.</returns>
        private async Task<Transaction> CreateTransaction(int walletId, string description, int amount, TransactionType type, DateTime? date = null)
        {
            var transaction = new Transaction
            {
                Description = description,
                TransactionSum = amount,
                Type = type,
                Date = date ?? DateTime.Now,
                WalletId = walletId
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}