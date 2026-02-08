using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Transaction;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Repositories;
using WebApplication3.Services;

namespace FinancialWebApplication.Tests.Unit.Services
{
    /// <summary>
    /// Тесты для <see cref="TransactionService"/>.
    /// </summary>
    public class TransactionServiceTests
    {
        /// <summary>
        /// Мок репозитория транзакций.
        /// </summary>
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;

        /// <summary>
        /// Мок логгера сервиса транзакций.
        /// </summary>
        private readonly Mock<ILogger<TransactionService>> _mockLogger;

        /// <summary>
        /// Экземпляр тестируемого сервиса транзакций.
        /// </summary>
        private readonly TransactionService _transactionService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TransactionServiceTests"/>.
        /// </summary>
        public TransactionServiceTests()
        {
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockLogger = new Mock<ILogger<TransactionService>>();

            _transactionService = new TransactionService(
                _mockTransactionRepository.Object,
                _mockLogger.Object
            );
        }

        /// <summary>
        /// Проверяет успешное создание транзакции с валидными данными.
        /// </summary>
        [Fact]
        public async Task CreateTransaction_ValidData_CreatesAndReturnsTransaction()
        {
            var walletId = 1;
            var expectedDate = new DateTime(2024, 3, 15);

            var inputDto = new CreateTransactionInputDto
            {
                Description = "Покупка продуктов",
                Sum = 1500,
                Date = expectedDate,
                Type = TransactionType.Expense
            };

            var createdTransaction = new Transaction
            {
                Id = 0,
                Description = inputDto.Description,
                TransactionSum = inputDto.Sum,
                Date = expectedDate,
                Type = inputDto.Type,
                WalletId = walletId
            };

            _mockTransactionRepository
                .Setup(r => r.AddTransaction(It.IsAny<Transaction>()))
                .ReturnsAsync(createdTransaction);

            // Act
            var result = await _transactionService.CreateTransaction(walletId, inputDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(0);
            result.Description.Should().Be("Покупка продуктов");
            result.Sum.Should().Be(1500);
            result.Date.Should().Be(expectedDate);
            result.Type.Should().Be(TransactionType.Expense);

            _mockTransactionRepository.Verify(r =>
                r.AddTransaction(It.IsAny<Transaction>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет успешное получение существующей транзакции.
        /// </summary>
        [Fact]
        public async Task GetTransaction_ExistingTransaction_ReturnsTransactionDto()
        {
            var transactionId = 1;
            var transactionFromRepo = new Transaction
            {
                Id = transactionId,
                Description = "Зарплата",
                TransactionSum = 50000,
                Date = new DateTime(2024, 3, 1),
                Type = TransactionType.Income
            };

            _mockTransactionRepository
                .Setup(r => r.GetTransaction(transactionId))
                .ReturnsAsync(transactionFromRepo);

            var result = await _transactionService.GetTransaction(transactionId);

            result.Should().NotBeNull();
            result.Description.Should().Be("Зарплата");
            result.Sum.Should().Be(50000);
            result.Date.Should().Be(new DateTime(2024, 3, 1));
            result.Type.Should().Be(TransactionType.Income);

            _mockTransactionRepository.Verify(r => r.GetTransaction(transactionId), Times.Once);
        }

        /// <summary>
        /// Проверяет, что при попытке получить несуществующую транзакцию выбрасывается исключение <see cref="TransactionNotFoundException"/>.
        /// </summary>
        [Fact]
        public async Task GetTransaction_NonExistingTransaction_ThrowsException()
        {
            var transactionId = 999;

            _mockTransactionRepository
                .Setup(r => r.GetTransaction(transactionId))
                .ThrowsAsync(new TransactionNotFoundException());

            await Assert.ThrowsAsync<TransactionNotFoundException>(() =>
                _transactionService.GetTransaction(transactionId));
        }

        /// <summary>
        /// Проверяет получение всех транзакций без фильтра с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_NoFilter_ReturnsPagedTransactions()
        {
            var walletId = 1;
            var pageNumber = 1;
            var pageSize = 10;

            var transactionsFromRepo = new List<Transaction>
            {
                new Transaction
                {
                    Id = 1,
                    Description = "Покупка",
                    TransactionSum = 1500,
                    Date = new DateTime(2024, 3, 15),
                    Type = TransactionType.Expense
                },
                new Transaction
                {
                    Id = 2,
                    Description = "Зарплата",
                    TransactionSum = 50000,
                    Date = new DateTime(2024, 3, 1),
                    Type = TransactionType.Income
                }
            };

            var totalCount = 2;

            _mockTransactionRepository
                .Setup(r => r.GetAllWalletTransactions(walletId, null, pageNumber, pageSize))
                .ReturnsAsync((transactionsFromRepo, totalCount)); // Кортеж!

            var result = await _transactionService.GetAllTransactions(walletId, null, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.PageNumber.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);
            result.TotalRecords.Should().Be(totalCount);

            result.Data[0].Description.Should().Be("Покупка");
            result.Data[0].Sum.Should().Be(1500);
            result.Data[0].Type.Should().Be(TransactionType.Expense);

            result.Data[1].Description.Should().Be("Зарплата");
            result.Data[1].Sum.Should().Be(50000);
            result.Data[1].Type.Should().Be(TransactionType.Income);

            _mockTransactionRepository.Verify(r =>
                r.GetAllWalletTransactions(walletId, null, pageNumber, pageSize),
                Times.Once);
        }

        /// <summary>
        /// Проверяет получение транзакций с применением фильтра.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_WithFilter_ReturnsFilteredTransactions()
        {
            var walletId = 1;
            var filter = new TransactionFilter
            {
                TransactionType = TransactionType.Income,
                MinAmount = 1000
            };
            var pageNumber = 1;
            var pageSize = 10;

            var transactionsFromRepo = new List<Transaction>
            {
                new Transaction
                {
                    Id = 1,
                    Description = "Зарплата",
                    TransactionSum = 50000,
                    Date = new DateTime(2024, 3, 1),
                    Type = TransactionType.Income
                }
            };

            var totalCount = 1;

            _mockTransactionRepository
                .Setup(r => r.GetAllWalletTransactions(walletId, filter, pageNumber, pageSize))
                .ReturnsAsync((transactionsFromRepo, totalCount));

            var result = await _transactionService.GetAllTransactions(walletId, filter, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].Type.Should().Be(TransactionType.Income);
            result.Data[0].Sum.Should().BeGreaterThanOrEqualTo(1000);

            _mockTransactionRepository.Verify(r =>
                r.GetAllWalletTransactions(walletId, filter, pageNumber, pageSize),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что при отсутствии транзакций возвращается пустой ответ с пагинацией.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_EmptyResult_ReturnsEmptyPagedResponse()
        {
            var walletId = 1;
            var pageNumber = 1;
            var pageSize = 10;

            var transactionsFromRepo = new List<Transaction>(); // Пустой список
            var totalCount = 0;

            _mockTransactionRepository
                .Setup(r => r.GetAllWalletTransactions(walletId, null, pageNumber, pageSize))
                .ReturnsAsync((transactionsFromRepo, totalCount));

            var result = await _transactionService.GetAllTransactions(walletId, null, pageNumber, pageSize);

            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.TotalRecords.Should().Be(0);
        }

        /// <summary>
        /// Проверяет получение сгруппированных и отсортированных транзакций за указанный месяц.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_ValidData_ReturnsGroupedAndSortedTransactions()
        {
            var walletId = 1;
            var year = 2024;
            var month = 3;

            var transactionsFromRepo = new List<Transaction>
            {
                new Transaction
                {
                    Id = 1,
                    Description = "Большая покупка",
                    TransactionSum = 10000,
                    Date = new DateTime(2024, 3, 15),
                    Type = TransactionType.Expense
                },
                new Transaction
                {
                    Id = 2,
                    Description = "Маленькая покупка",
                    TransactionSum = 1000,
                    Date = new DateTime(2024, 3, 10),
                    Type = TransactionType.Expense
                },
                new Transaction
                {
                    Id = 3,
                    Description = "Зарплата",
                    TransactionSum = 50000,
                    Date = new DateTime(2024, 3, 1),
                    Type = TransactionType.Income
                }
            };

            _mockTransactionRepository
                .Setup(r => r.GetGroupAndSortTransactions(walletId, year, month))
                .ReturnsAsync(transactionsFromRepo);

            var result = await _transactionService.GetGroupAndSortTransactions(walletId, year, month);

            result.Should().NotBeNull();
            result.Should().HaveCount(3);

            result[0].Description.Should().Be("Большая покупка");
            result[0].Sum.Should().Be(10000);
            result[0].Type.Should().Be(TransactionType.Expense);

            result[1].Description.Should().Be("Маленькая покупка");
            result[1].Sum.Should().Be(1000);

            result[2].Description.Should().Be("Зарплата");
            result[2].Sum.Should().Be(50000);
            result[2].Type.Should().Be(TransactionType.Income);

            _mockTransactionRepository.Verify(r =>
                r.GetGroupAndSortTransactions(walletId, year, month),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что при отсутствии транзакций за указанный месяц возвращается пустой список.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_NoTransactions_ReturnsEmptyList()
        {
            var walletId = 1;
            var year = 2024;
            var month = 3;

            var transactionsFromRepo = new List<Transaction>(); // Пустой список

            _mockTransactionRepository
                .Setup(r => r.GetGroupAndSortTransactions(walletId, year, month))
                .ReturnsAsync(transactionsFromRepo);

            var result = await _transactionService.GetGroupAndSortTransactions(walletId, year, month);

            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockTransactionRepository.Verify(r =>
                r.GetGroupAndSortTransactions(walletId, year, month),
                Times.Once);
        }

        /// <summary>
        /// Проверяет корректность маппинга полей при получении всех транзакций.
        /// </summary>
        [Fact]
        public async Task GetAllTransactions_MapsCorrectFields()
        {
            var walletId = 1;
            var transaction = new Transaction
            {
                Id = 1,
                Description = "Test",
                TransactionSum = 1000, 
                Date = DateTime.UtcNow,
                Type = TransactionType.Expense
            };

            _mockTransactionRepository
                .Setup(r => r.GetAllWalletTransactions(walletId, null, 1, 10))
                .ReturnsAsync((new List<Transaction> { transaction }, 1));

            var result = await _transactionService.GetAllTransactions(walletId);

            result.Data[0].Sum.Should().Be(1000);
            result.Data[0].Description.Should().Be("Test");
        }

        /// <summary>
        /// Проверяет корректность маппинга полей при получении сгруппированных транзакций.
        /// </summary>
        [Fact]
        public async Task GetGroupAndSortTransactions_MapsCorrectFields()
        {
            var walletId = 1;
            var transaction = new Transaction
            {
                Id = 1,
                Description = "Test",
                TransactionSum = 2000,
                Date = DateTime.UtcNow,
                Type = TransactionType.Income
            };

            _mockTransactionRepository
                .Setup(r => r.GetGroupAndSortTransactions(walletId, 2024, 3))
                .ReturnsAsync(new List<Transaction> { transaction });

            var result = await _transactionService.GetGroupAndSortTransactions(walletId, 2024, 3);

            result[0].Sum.Should().Be(2000); 
            result[0].Description.Should().Be("Test");
            result[0].Type.Should().Be(TransactionType.Income);
        }
    }
}