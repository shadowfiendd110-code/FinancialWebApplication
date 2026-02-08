using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Transaction;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Контроллер для работы с транзакциями.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User, Admin")]
    public class TransactionController : ControllerBase
    {
        /// <summary>
        /// Сервис для работы с транзакциями.
        /// </summary>
        public ITransactionService _transactionService;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<TransactionController> _logger;

        /// <summary>
        /// Помощник работы с кэшем.
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Создание контроллера.
        /// </summary>
        /// <param name="transactionService">Сервис для работы с транзакциями.</param>
        /// <param name="logger">Логгер.</param>
        /// <param name="cache">Помощник работы с кэшем.</param>
        public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger, IMemoryCache cache)
        {
            _transactionService = transactionService;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Получает все транзакции кошелька.
        /// </summary>
        /// <param name="walletId">Id Кошелька.</param>
        /// <param name="pageSize">Размер страницы в записях.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="filter">Фильтр по транзакциям.</param>
        /// <returns>Все транзакции кошелька.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTransactions(
            int walletId,
            [FromQuery] TransactionFilter? filter = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if(pageNumber < 1)
            {
                return BadRequest("Номер страницы должен быть больше 0");
            }

            if(pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Размер страницы должен быть от 1 до 100");
            }

            if (filter != null)
            {
                if (filter.DateFrom.HasValue & filter.DateTo.HasValue)
                {
                    if (filter.DateTo.Value < filter.DateFrom.Value)
                    {
                        return BadRequest("Дата 'с' не может быть позже даты 'по'");
                    }
                }

                // Теперь эти проверки безопасны, так как мы внутри if(filter != null)
                if (filter.MinAmount.HasValue && filter.MaxAmount.HasValue)
                {
                    if (filter.MinAmount > filter.MaxAmount)
                    {
                        return BadRequest("Минимальная сумма не может быть больше максимальной");
                    }
                }

                if (filter.HasTypeFilter &&
                    filter.TransactionType != Models.TransactionType.Income &&
                    filter.TransactionType != Models.TransactionType.Expense)
                {
                    return BadRequest("Тип транзакции должен быть 'Income' или 'Expense'");
                }
            }

            _logger.LogInformation(
                "HTTP GET /api/Transaction?walletId={WalletId}&pageNumber=" +
                "{PageNumber}&pageSize={PageSize}",
                walletId,
                pageNumber,
                pageSize);

            var pageResponse = await _transactionService.GetAllTransactions(walletId, filter, pageNumber, pageSize);

            _logger.LogInformation(
                "HTTP 200 OK для /api/Transaction?walletId={WalletId}. " +
                "Страница {PageNumber} из {TotalPages}, записей: {TransactionCount}",
                walletId, 
                pageResponse.PageNumber, 
                pageResponse.TotalPages, 
                pageResponse.Data.Count);

            return Ok(pageResponse);
        }

        /// <summary>
        /// Получает сгруппированные и отсортированные транзакции за определённый месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Сгруппированные и отсортированные транзакции за определённый месяц.</returns>
        [HttpGet("{walletId}/{year}/{month}")]
        public async Task<IActionResult> GetGroupAndSortedTransactions(int walletId, int year, int month)
        {
            var cacheKey = $"groupedTransactions_{walletId}_{month}_{year}";

            _logger.LogInformation(
                "HTTP GET /api/Transaction/{WalletId}/{Year}/{Month}",
                walletId, year, month);

            if (_cache.TryGetValue(cacheKey, out var cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Кэш: Взяли сгруппированные транзакции для WalletId: {WalletId} за {Month}/{Year}",
                    walletId,
                    month,
                    year);
                return Ok(cachedResult);
            }

            var result = await _transactionService.GetGroupAndSortTransactions(walletId, year, month);

            TimeSpan expiration = DateTime.Now.Year == year && DateTime.Now.Month == month
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromHours(24);

            _cache.Set(cacheKey, result, expiration);

            _logger.LogInformation(
                "HTTP 200 OK для /api/Transaction/{WalletId}/{Year}/{Month}. " +
                "Результатов: {ResultCount}",
                walletId, year, month, result.Count);

            return Ok(result);
        }

        /// <summary>
        /// Получает транзакцию.
        /// </summary>
        /// <param name="transactionId">Id транзакции.</param>
        /// <returns>Транзакцию.</returns>
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransaction(int transactionId)
        {
            _logger.LogInformation(
                "HTTP GET /api/Transaction/{TransactionId}",
                transactionId);

            var transaction = await _transactionService.GetTransaction(transactionId);

            _logger.LogInformation(
                "HTTP 200 OK для /api/Transaction/{TransactionId}",
                transactionId);

            return Ok(transaction);
        }

        /// <summary>
        /// Создаёт транзакцию.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="inputTransactionDto">Входной DTO транзакции.</param>
        /// <returns>Созданную транзакцию.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateTransaction(int walletId, CreateTransactionInputDto inputTransactionDto)
        {
            _logger.LogInformation(
                "HTTP POST /api/Transaction?walletId={WalletId}",
                walletId);

            var newTransaction = await _transactionService.CreateTransaction(walletId, inputTransactionDto);

            _logger.LogInformation(
                "HTTP 201 Created для /api/Transaction?walletId={WalletId}. " +
                "TransactionId: {TransactionId}",
                walletId, newTransaction.Id);

            return Created($"api/transaction/{newTransaction.Id}", newTransaction);
        }
    }
}