using WebApplication3.Models;
using WebApplication3.Repositories;
using WebApplication3.Models.Common;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Transaction;

namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс для работы с транзакциями.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Создаёт транзакцию.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="inputTransactionDto">Входной DTO транзакции.</param>
        /// <returns>Созданную транзакцию.</returns>
        Task<CreateTransactionOutputDto> CreateTransaction(int walletId, CreateTransactionInputDto inputTransactionDto);

        /// <summary>
        /// Получает транзакцию.
        /// </summary>
        /// <param name="transactionId">Id транзакции.</param>
        /// <returns>Транзакцию.</returns>
        Task<TransactionDto> GetTransaction(int transactionId);

        /// <summary>
        /// Получает все транзакции кошелька.
        /// </summary>
        /// <param name="walletId">Id Кошелька.</param>
        /// <param name="filter">Фильтр.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <returns>Все транзакции кошелька.</returns>
        public Task<PagedResponse<TransactionDto>> GetAllTransactions(
            int walletId, TransactionFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Получает сгруппированные и отсортированные транзакции за определённый месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Сгруппированные и отсортированные транзакции за определённый месяц.</returns>
        Task<List<TransactionDto>> GetGroupAndSortTransactions(int walletId, int year, int month);
    }

    /// <summary>
    /// Сервис для работы с транзакциями.
    /// </summary>
    public class TransactionService: ITransactionService
    {
        /// <summary>
        /// Репозиторий для работы с транзакциями.
        /// </summary>
        private readonly ITransactionRepository _transactionRepository;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<TransactionService> _logger;

        /// <summary>
        /// Создание сервиса.
        /// </summary>
        /// <param name="transactionRepository">Репозиторий для работы с транзакциями.</param>
        /// <param name="logger">Логгер сервиса.</param>
        public TransactionService(ITransactionRepository transactionRepository, ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        /// <summary>
        /// Получает сгруппированные и отсортированные транзакции за определённый месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Сгруппированные и отсортированные транзакции за определённый месяц.</returns>
        public async Task<List<TransactionDto>> GetGroupAndSortTransactions(int walletId, int year, int month)
        {
            _logger.LogInformation("Запрос транзакций: WalletId={WalletId}, {Month}/{Year}",
                walletId, 
                month, 
                year);

            var transactions = await _transactionRepository.GetGroupAndSortTransactions(walletId, year, month);

            _logger.LogInformation("Получено {TransactionsCount} транзакций для WalletId={WalletId} за {Month}/{Year}",
                transactions.Count, 
                walletId, 
                month, 
                year);

            return transactions.Select(transaction => new TransactionDto
            {
                Sum = transaction.TransactionSum,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type,
            }).ToList();
        }

        /// <summary>
        /// Получает все транзакции кошелька.
        /// </summary>
        /// <param name="walletId">Id Кошелька.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр по транзакциям.</param>
        /// <returns>Все транзакции кошелька.</returns>
        public async Task<PagedResponse<TransactionDto>> GetAllTransactions (
            int walletId, TransactionFilter? filter = null, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation(
                "Получение транзакций. WalletId: {WalletId}, " +
                "Фильтр: {Filter}, Страница: {PageNumber}, Размер: {PageSize}",
                walletId,
                filter?.ToString() ?? "без фильтра",
                pageNumber,
                pageSize);

            var (transactions, totalCount) = await _transactionRepository.GetAllWalletTransactions(walletId, filter, pageNumber, pageSize);

            _logger.LogInformation("Получено {TransactionCount} транзакций для WalletId: {WalletId}. Всего: {TotalCount}",
                transactions.Count, 
                walletId,
                totalCount);

            var transactionDtos = transactions.Select(transaction => new TransactionDto
            {
                Sum = transaction.TransactionSum,
                Description = transaction.Description,
                Date = transaction.Date,
                Type = transaction.Type,
            }).ToList();

            return new PagedResponse<TransactionDto>(   
                transactionDtos,
                pageNumber,
                pageSize,
                totalCount);
        }

        /// <summary>
        /// Получает транзакцию.
        /// </summary>
        /// <param name="transactionId">Id транзакции.</param>
        /// <returns>Транзакцию.</returns>
        public async Task<TransactionDto> GetTransaction(int transactionId)
        {
            _logger.LogInformation("Получение транзакции по TransactionId: {TransactionId}.", 
                transactionId);

            var transaction = await _transactionRepository.GetTransaction(transactionId);

            _logger.LogInformation(
            "Транзакция TransactionId={TransactionId} получена",
                transaction.Id);

            return new TransactionDto
            {
                Description = transaction.Description,
                Sum = transaction.TransactionSum,
                Date = transaction.Date,
                Type = transaction.Type,
            };
        }

        /// <summary>
        /// Создаёт транзакцию.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="input">Входной DTO транзакции.</param>
        /// <returns>Созданную транзакцию.</returns>
        public async Task<CreateTransactionOutputDto> CreateTransaction(int walletId, CreateTransactionInputDto input)
        {
            var newTransaction = new Transaction
            {
                TransactionSum = input.Sum,
                Description = input.Description,
                Date = input.Date,
                Type = input.Type,
                WalletId = walletId,
            };

            _logger.LogInformation("Создание транзакции. WalletId={WalletId}, Тип={TransactionType}",
                walletId,
                input.Type);

            var addedTransaction = await _transactionRepository.AddTransaction(newTransaction);

            _logger.LogInformation("Транзакция TransactionId: {TransactionId} для WalletId: {WalletId}",
                addedTransaction.Id,
                walletId);

            return new CreateTransactionOutputDto
            {
                Sum = addedTransaction.TransactionSum,
                Date = addedTransaction.Date,
                Description = addedTransaction.Description,
                Type = addedTransaction.Type,
            };
        }
    }
}
