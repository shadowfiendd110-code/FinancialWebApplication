using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Repositories;
using WebApplication3.DTOs.Filters;
using WebApplication3.Data;

namespace WebApplication3.Tests.Repositories
{
    /// <summary>
    /// Тесты для <see cref="TransactionRepository"/>.
    /// </summary>
    public class TransactionRepositoryTests : IDisposable
    {
        /// <summary>
        /// Контекст базы данных для тестирования.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Экземпляр тестируемого репозитория транзакций.
        /// </summary>
        private readonly TransactionRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TransactionRepositoryTests"/>.
        /// </summary>
        public TransactionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TransactionRepository(_context);
        }

        /// <summary>
        /// Освобождает ресурсы тестового класса.
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
        }

        /// <summary>
        /// Проверяет успешное добавление транзакции в базу данных.
        /// </summary>
        [Fact]
        public async Task AddTransaction_ValidTransaction_SavesAndReturnsTransaction()
        {
            var transaction = CreateTestTransaction("Тестовая транзакция", DateTime.Now, 1000, TransactionType.Income);

            var result = await _repository.AddTransaction(transaction);

            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Description.Should().Be("Тестовая транзакция");
            result.TransactionSum.Should().Be(1000);
            result.Type.Should().Be(TransactionType.Income);

            var saved = await _context.Transactions.FindAsync(result.Id);
            saved.Should().NotBeNull();
            saved.TransactionSum.Should().Be(1000);
        }

        /// <summary>
        /// Проверяет успешное получение существующей транзакции по идентификатору.
        /// </summary>
        [Fact]
        public async Task GetTransaction_ExistingId_ReturnsTransaction()
        {
            var transaction = CreateTestTransaction("Существующая транзакция", DateTime.Now, 500, TransactionType.Expense);
            await _repository.AddTransaction(transaction);

            var result = await _repository.GetTransaction(transaction.Id);

            result.Should().NotBeNull();
            result.Description.Should().Be("Существующая транзакция");
            result.Id.Should().Be(transaction.Id);
            result.TransactionSum.Should().Be(500);
        }

        /// <summary>
        /// Проверяет, что при попытке получения несуществующей транзакции выбрасывается исключение <see cref="TransactionNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetTransaction_NonExistingId_ThrowsTransactionNotFoundException()
        {
            var action = async () => await _repository.GetTransaction(999);
            await action.Should().ThrowAsync<TransactionNotFoundException>()
                .WithMessage("Транзакция не найдена");
        }

        /// <summary>
        /// Проверяет получение всех транзакций кошелька без фильтра с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_NoFilter_ReturnsAllTransactionsWithCorrectCount()
        {
            var walletId = 1;
            await SeedTransactions(walletId, 25);

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, pageNumber: 1, pageSize: 10);

            transactions.Should().HaveCount(10);
            totalCount.Should().Be(25);
            transactions.Should().OnlyContain(t => t.WalletId == walletId);
        }

        /// <summary>
        /// Проверяет получение транзакций кошелька с фильтром по типу.
        /// </summary>
        [Theory]
        [InlineData(TransactionType.Income)]
        [InlineData(TransactionType.Expense)]
        public async Task GetAllWalletTransactions_TypeFilter_ReturnsFilteredTransactions(TransactionType type)
        {
            var walletId = 1;
            await SeedTransactions(walletId, 10, 100, type);
            await SeedTransactions(walletId, 5, 100, type == TransactionType.Income ? TransactionType.Expense : TransactionType.Income);

            var filter = new TransactionFilter { TransactionType = type };

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, filter);

            transactions.Should().HaveCountLessThanOrEqualTo(10);
            totalCount.Should().Be(10);
            transactions.Should().OnlyContain(t => t.Type == type);
        }

        /// <summary>
        /// Проверяет получение транзакций кошелька с фильтром по диапазону дат.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_DateRangeFilter_ReturnsFilteredByDate()
        {
            var walletId = 1;
            var startDate = new DateTime(2026, 1, 15);
            var endDate = new DateTime(2026, 1, 20);

            await SeedTransactions(walletId, 5, 100, TransactionType.Income, startDate.AddDays(-5));
            await SeedTransactions(walletId, 6, 100, TransactionType.Income, startDate);
            await SeedTransactions(walletId, 5, 100, TransactionType.Income, endDate.AddDays(1));

            var filter = new TransactionFilter { DateFrom = startDate, DateTo = endDate };

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, filter);

            totalCount.Should().Be(6);
            transactions.Should().OnlyContain(t => t.Date >= startDate && t.Date <= endDate);
        }

        /// <summary>
        /// Проверяет получение транзакций кошелька с фильтром по сумме.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_AmountFilter_ReturnsFilteredByAmount()
        {
            var walletId = 1;
            await SeedTransactions(walletId, 3, 100);
            await SeedTransactions(walletId, 5, 500);
            await SeedTransactions(walletId, 2, 1000);

            var filter = new TransactionFilter { MinAmount = 200, MaxAmount = 800 };

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, filter);

            totalCount.Should().Be(5);
            transactions.Should().OnlyContain(t => t.TransactionSum == 500);
        }

        /// <summary>
        /// Проверяет получение транзакций кошелька с фильтром по описанию.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_DescriptionFilter_ReturnsFilteredByDescription()
        {
            var walletId = 1;
            await SeedTransactions(walletId, 3, 100, TransactionType.Income, description: "Зарплата");
            await SeedTransactions(walletId, 3, 100, TransactionType.Income, description: "Еда");
            await SeedTransactions(walletId, 3, 100, TransactionType.Income, description: "Зарплата бонус");

            var filter = new TransactionFilter { DescriptionContains = "Зарплата" };

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, filter);

            totalCount.Should().Be(6);
            transactions.Should().OnlyContain(t => t.Description.Contains("Зарплата"));
        }

        /// <summary>
        /// Проверяет получение транзакций кошелька с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_Pagination_ReturnsCorrectPage()
        {
            var walletId = 1;
            await SeedTransactions(walletId, 25);

            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(walletId, pageNumber: 2, pageSize: 10);

            transactions.Should().HaveCount(10);
            totalCount.Should().Be(25);
        }

        /// <summary>
        /// Проверяет получение транзакций для кошелька без транзакций.
        /// </summary>
        [Fact]
        public async Task GetAllWalletTransactions_WalletWithNoTransactions_ReturnsEmptyList()
        {
            var (transactions, totalCount) = await _repository.GetAllWalletTransactions(999);

            transactions.Should().BeEmpty();
            totalCount.Should().Be(0);
        }

        /// <summary>
        /// Проверяет получение сгруппированных и отсортированных транзакций за указанный месяц.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_ValidMonth_ReturnsGroupedAndSorted()
        {
            var walletId = 1;
            var testDate = new DateTime(2026, 1, 15);

            var income1 = CreateTestTransaction("Доход 1", testDate, 1000, TransactionType.Income, walletId);
            var income2 = CreateTestTransaction("Доход 2", testDate.AddDays(1), 2000, TransactionType.Income, walletId);
            var expense1 = CreateTestTransaction("Расход 1", testDate.AddDays(2), 500, TransactionType.Expense, walletId);
            var expense2 = CreateTestTransaction("Расход 2", testDate.AddDays(3), 300, TransactionType.Expense, walletId);

            await _context.Transactions.AddRangeAsync(income1, income2, expense1, expense2);
            await _context.SaveChangesAsync();

            var result = await _repository.GetGroupAndSortTransactions(walletId, 2026, 1);

            result.Should().HaveCount(4);

            result[0].Type.Should().Be(TransactionType.Income);
            result[0].TransactionSum.Should().Be(2000);
            result[1].Type.Should().Be(TransactionType.Income);
            result[1].TransactionSum.Should().Be(1000);

            result[2].Type.Should().Be(TransactionType.Expense);
            result[2].TransactionSum.Should().Be(500);
            result[3].Type.Should().Be(TransactionType.Expense);
            result[3].TransactionSum.Should().Be(300);
        }

        /// <summary>
        /// Проверяет получение сгруппированных транзакций за месяц без транзакций.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_EmptyMonth_ReturnsEmptyList()
        {
            var result = await _repository.GetGroupAndSortTransactions(1, 2026, 1);

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Проверяет получение сгруппированных транзакций для несуществующего кошелька.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_WrongWalletId_ReturnsEmptyList()
        {
            await SeedTransactions(1, 5);

            var result = await _repository.GetGroupAndSortTransactions(999, 2026, 1);

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Создает тестовую транзакцию.
        /// </summary>
        /// <param name="description">Описание транзакции.</param>
        /// <param name="date">Дата транзакции.</param>
        /// <param name="sum">Сумма транзакции.</param>
        /// <param name="type">Тип транзакции.</param>
        /// <param name="walletId">Идентификатор кошелька.</param>
        /// <returns>Созданная транзакция.</returns>
        private Transaction CreateTestTransaction(string description, DateTime date, int sum, TransactionType type, int walletId = 1)
        {
            return new Transaction(description, date, sum, type)
            {
                WalletId = walletId
            };
        }

        /// <summary>
        /// Создает тестовые транзакции в базе данных.
        /// </summary>
        /// <param name="walletId">Идентификатор кошелька.</param>
        /// <param name="count">Количество транзакций.</param>
        /// <param name="transactionSum">Сумма транзакций.</param>
        /// <param name="type">Тип транзакций.</param>
        /// <param name="startDate">Начальная дата.</param>
        /// <param name="description">Описание транзакций.</param>
        private async Task SeedTransactions(int walletId, int count,
            int transactionSum = 100,
            TransactionType type = TransactionType.Income,
            DateTime startDate = default,
            string description = "Тест")
        {
            var baseDate = startDate == default(DateTime) ? DateTime.Now.AddDays(-count) : startDate;

            var transactions = Enumerable.Range(0, count)
                .Select(i => CreateTestTransaction(description, baseDate.AddDays(i), transactionSum, type, walletId))
                .ToList();

            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
        }
    }
}