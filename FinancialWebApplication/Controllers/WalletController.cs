using Microsoft.AspNetCore.Mvc;
using WebApplication3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Wallet;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Контроллер сущности Кошелёк.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User, Admin")]
    public class WalletController : ControllerBase
    {
        /// <summary>
        /// Сервис для работы с кошельками.
        /// </summary>
        public IWalletService _walletService;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<WalletController> _logger;

        /// <summary>
        /// Помощник работы с кэшем.
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Создание контроллера.
        /// </summary>
        /// <param name="walletService">Сервис для работы с кошельками.</param>
        /// <param name="logger">Логгер.</param>
        /// <param name="cache">Помощник работы с кэшем.</param>
        public WalletController(IWalletService walletService, ILogger<WalletController> logger, IMemoryCache cache)
        {
            _walletService = walletService;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Получает топ 3 траты кошелька за определённый месяц.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Топ 3 траты кошелька за определённый месяц.</returns>
        [HttpGet("User/{userId}/top-three-expense/{year}/{month}")]
        public async Task<IActionResult> GetTopThreeExpensePerWallet(int userId, int year, int month)
        {
            var cacheKey = $"topThreeExpensesPerWallet_{userId}_{month}_{year}";

            _logger.LogInformation(
                "HTTP GET /api/Wallet/User/{UserId}/top-three-expense/{Year}/{Month}",
                userId,
                year,
                month);

            if (_cache.TryGetValue(cacheKey, out var cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Кэш: Взяли топ-3 траты для кошелька.");
                return Ok(cachedResult);
            }

            var result = await _walletService.GetTopThreeExpensePerWallet(userId, year, month);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(60));

            return Ok(result);
        }

        /// <summary>
        /// Получает доходы и расходы кошелька за конкретный месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Доходы и расходы кошелька за конкретный месяц.</returns>
        [HttpGet("{walletId}/monthly-summary/{year}/{month}")]
        public async Task<IActionResult> GetWalletMonthlySummary(int walletId, int year, int month)
        {
            var cacheKey = $"monthlySummary_{walletId}_{month}_{year}";

            _logger.LogInformation(
                "HTTP GET /api/Wallet/{WalletId}/monthly-summary/{Year}/{Month}",
                walletId,
                year,
                month);

            if (_cache.TryGetValue(cacheKey, out var cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Кэш: Взяли доходы/расходы для WalletId: {WalletId} за {Month}/{Year}.",
                    walletId,
                    month,
                    year);
                return Ok(cachedResult);
            }

            var summary = await _walletService.GetWalletMonthlySummary(walletId, year, month);
            _cache.Set(cacheKey, summary, TimeSpan.FromHours(2));

            return Ok(summary);
        }

        /// <summary>
        /// Удаляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        [HttpDelete("{walletId}")]
        public async Task<IActionResult> Delete(int walletId)
        {
            _logger.LogInformation("HTTP DELETE /api/Wallet/{WalletId}", walletId);
            await _walletService.DeleteWallet(walletId);
            return NoContent();
        }

        /// <summary>
        /// Обновляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="wallet">DTO обновляемого кошелька</param>
        /// <returns>Обновлённый кошелёк.</returns>
        [HttpPut("{walletId}")]
        public async Task<IActionResult> UpdateWallet(int walletId, UpdateWalletDto wallet)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                "HTTP PUT /api/Wallet/{WalletId}. Новые данные: Имя='{WalletName}', Валюта={Currency}",
                walletId,
                wallet.Name,
                wallet.Currency);

            var updatedWallet = await _walletService.UpdateWallet(walletId, wallet);
            return Ok(updatedWallet);
        }

        /// <summary>
        /// Получает все кошельки пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="filter">Фильтр.</param>
        /// <param name="pageSize">Размер страницы в записях.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <returns>Все кошельки пользователя.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUserWallets(
            [FromQuery] int userId,
            [FromQuery] WalletFilter? filter = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                return BadRequest("Номер страницы должен быть больше 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Размер страницы должен быть от 1 до 100");
            }

            if (filter != null)
            {
                if (filter.CurrentBalance.HasValue && filter.CurrentBalance.Value < 0)
                {
                    return BadRequest("Текущий баланс не может быть меньше 0.");
                }

                if (filter.HasCurrencyEquals && filter.CurrencyEquals!.Length > 10)
                {
                    return BadRequest("Код валюты не должен превышать 10 символов.");
                }

                if (filter.HasNameEquals && filter.NameEquals!.Length > 100)
                {
                    return BadRequest("Название кошелька не должно превышать 100 символов.");
                }
            }

            _logger.LogInformation("HTTP GET /api/Wallet?userId={UserId}&pageNumber={PageNumber}&pageSize={PageSize}",
                userId,
                pageNumber,
                pageSize);

            var wallets = await _walletService.GetAllUserWallets(userId, filter, pageNumber, pageSize);
            return Ok(wallets);
        }

        /// <summary>
        /// Получает кошелёк.
        /// </summary>
        /// <param name="id">Id кошелька.</param>
        /// <returns>Кошелёк.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWallet(int id)
        {
            _logger.LogInformation("HTTP GET /api/Wallet/{WalletId}", id);
            var wallet = await _walletService.GetWallet(id);
            return Ok(wallet);
        }

        /// <summary>
        /// Создает кошелёк.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="createdWalletInputDto">Входной DTO кошелька.</param>
        /// <returns>Созданный кошелёк.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateWallet(
            [FromQuery] int userId,
            [FromBody] CreateWalletInputDto createdWalletInputDto)
        {
            _logger.LogInformation(
                "HTTP POST /api/Wallet?userId={UserId}. Данные: Имя='{WalletName}', Валюта={Currency}",
                userId,
                createdWalletInputDto.Name,
                createdWalletInputDto.Currency);

            var newWallet = await _walletService.CreateWallet(userId, createdWalletInputDto);
            return Created($"api/wallet/{newWallet.Id}", newWallet);
        }
    }
}