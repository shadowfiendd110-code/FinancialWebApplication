using WebApplication3.DTOs.Filters;
using WebApplication3.DTOs.Transaction;
using WebApplication3.DTOs.Wallet;
using WebApplication3.Exceptions;
using WebApplication3.Models;
using WebApplication3.Models.Common;
using WebApplication3.Repositories;

namespace WebApplication3.Services
{
    /// <summary>
    /// Интерфейс для работы с кошельками.
    /// </summary>
    public interface IWalletService
    {
        /// <summary>
        /// Создает кошелёк.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="createdWalletInputDto">Входной DTO кошелька.</param>
        /// <returns>Созданный кошелёк.</returns>
        Task<CreatedWalletOutputDto> CreateWallet(int userId, CreateWalletInputDto createdWalletInputDto);

        /// <summary>
        /// Получает кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Кошелёк.</returns>
        Task<WalletDto> GetWallet(int walletId);

        /// <summary>
        /// Получает все кошельки пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для кошельков.</param>
        /// <returns>Все кошельки пользователя.</returns>
        public Task<PagedResponse<WalletDto>> GetAllUserWallets(
        int userId, WalletFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Обновляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="updatedWalletDto">DTO обновляемого кошелька</param>
        /// <returns>Обновлённый кошелёк.</returns>
        Task<UpdateWalletDto> UpdateWallet(int walletId, UpdateWalletDto updatedWalletDto);

        /// <summary>
        /// Удаляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        Task DeleteWallet(int walletId);

        /// <summary>
        /// Получает доходы и расходы кошелька за конкретный месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Доходы и расходы кошелька за конкретный месяц.</returns>
        Task<WalletMonthlySummaryDto> GetWalletMonthlySummary(int walletId, int year, int month);

        /// <summary>
        /// Получает топ 3 траты кошелька за определённый месяц.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Топ 3 траты кошелька за определённый месяц.</returns>
        Task<List<WalletTopExpensesDto>> GetTopThreeExpensePerWallet(int userId, int year, int month); 
    }

    /// <summary>
    /// Сервис для работы с кошельками.
    /// </summary>
    public class WalletService: IWalletService
    {
        /// <summary>
        /// Репозиторий для работы с кошельками.
        /// </summary>
        private readonly IWalletRepository _walletRepository;

        /// <summary>
        /// Логгер сервиса.
        /// </summary>
        private readonly ILogger<WalletService> _logger;

        /// <summary>
        /// Создание сервиса.
        /// </summary>
        /// <param name="walletRepository">Репозиторий для работы с кошельками.</param>
        /// <param name="logger">Логгер сервиса.</param>
        public WalletService(IWalletRepository walletRepository, ILogger<WalletService> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        /// <summary>
        /// Получает топ 3 траты кошелька за определённый месяц.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Топ 3 траты кошелька за определённый месяц.</returns>
        public async Task<List<WalletTopExpensesDto>> GetTopThreeExpensePerWallet(int userId, int year, int month)
        {
            _logger.LogInformation("Получение топ-3 трат для кошелька у UserId: {UserId} за {Month}/{Year}",
                userId,
                month,
                year);

            var topThreeExpenses = await _walletRepository.GetTopThreeExpensesPerWallet(userId, year, month);

            _logger.LogInformation("Получены топ-3 траты для кошелька у UserId: {UserId} за {Month}/{Year}",
                userId,
                month,
                year);

            _logger.LogInformation("Получение всех кошельков UserId: {UserId}",
                userId);

            var (wallets, _) = await _walletRepository.GetAllUserWallets(userId, null, 1, int.MaxValue);

            _logger.LogInformation("Получены все кошельки UserId: {UserId}. Количество: {WalletsCount}",
                userId,
                wallets.Count);

            var result = new List<WalletTopExpensesDto>();

            foreach (var wallet in wallets)
            {
                var walletDto = new WalletTopExpensesDto
                {
                    Name = wallet.Name,
                    Id = wallet.Id,
                    Currency = wallet.Currency,
                    TopThreeExpenses = topThreeExpenses.TryGetValue(wallet.Id, out var transactions)
                        ? transactions.Select(t => new TransactionDto
                        {
                            Id = t.Id,
                            Sum = t.TransactionSum,
                            Description = t.Description,
                            Date = t.Date,
                            Type = t.Type
                        }).ToList()
                        : new List<TransactionDto>()
                };

                result.Add(walletDto);
            }

            return result;
        }

        /// <summary>
        /// Получает доходы и расходы кошелька за конкретный месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Доходы и расходы кошелька за конкретный месяц.</returns>
        public async Task<WalletMonthlySummaryDto> GetWalletMonthlySummary(int walletId, int year, int month)
        {
            _logger.LogInformation("Получение доходов/расходов для WalletId: {WalletId} за {Month}/{Year}", 
                walletId, 
                month, 
                year);

            var incomeExpense = await _walletRepository.GetWalletMonthlySummary(walletId, year, month);

            _logger.LogInformation("Получены доходы/расходы для WalletId: {WalletId} за {Month}/{Year}," +
            "Валюта: {Currency}", 
                walletId, 
                month, 
                year, 
            incomeExpense.Currency);

            return new WalletMonthlySummaryDto
            {
                Id = walletId,
                Income = incomeExpense.Income,
                Expense = incomeExpense.Expense,
                Year = year,
                Month = month,
                Name = incomeExpense.Name,
                Currency = incomeExpense.Currency,
            };
        }

        /// <summary>
        /// Удаляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        public async Task DeleteWallet(int walletId)
        {
            _logger.LogInformation("Удаление кошелька по WalletId: {WalletId}", walletId);

            await _walletRepository.DeleteWallet(walletId);

            _logger.LogInformation("Удалён кошелёк по WalletId: {WalletId}", walletId);
        }

        /// <summary>
        /// Обновляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="walletDto">DTO обновляемого кошелька</param>
        /// <returns>Обновлённый кошелёк.</returns>
        public async Task<UpdateWalletDto> UpdateWallet(int walletId, UpdateWalletDto walletDto)
        {
            _logger.LogInformation("Получение кошелька по WalletId: {WalletId}", walletId);

            var existingWallet = await _walletRepository.GetWallet(walletId);

            _logger.LogInformation("Получен кошелёк. WalletId: {WalletId} WalletName: {WalletName} WalletCurrency: {WalletCurrency}",
                existingWallet.Id, 
                existingWallet.Name, 
                existingWallet.Currency);

            existingWallet.Currency = walletDto.Currency;
            existingWallet.Name = walletDto.Name;

            _logger.LogInformation("Обновление кошелька по WalletId: {WalletId}", walletId);

            await _walletRepository.UpdateWallet(existingWallet);

            _logger.LogInformation("Обновлён кошелёк. WalletId: {WalletId} WalletName: {WalletName} WalletCurrency: {WalletCurrency}",
                existingWallet.Id,
                existingWallet.Name,
                existingWallet.Currency);

            return new UpdateWalletDto
            {
                Currency = existingWallet.Currency,
                Name = existingWallet.Name,
            };
        }

        /// <summary>
        /// Получает все кошельки пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для кошельков.</param>
        /// <returns>Все кошельки пользователя.</returns>
        public async Task<PagedResponse<WalletDto>> GetAllUserWallets(
        int userId, WalletFilter? filter = null, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation("Получение всех кошельков по UserId: {UserId}. " +
            "Фильтр: {Filter} Количество: {PageSize} Страница: {PageNumber}", 
                userId,
                filter?.ToString() ?? "без фильтра",
                pageSize,
                pageNumber);

            var (wallets, totalCount) = await _walletRepository.GetAllUserWallets(userId, filter, pageNumber, pageSize);

            _logger.LogInformation("Получено {WalletsCount} кошельков для UserId: {UserId}. Всего кошельков: {TotalWalletsCount}", 
                wallets.Count,
                userId,
                totalCount);

            _logger.LogInformation("Получение балансов всех кошельков по UserId: {UserId}", userId);

            var balances = await _walletRepository.GetAllWalletsCurrentBalance(userId);

            _logger.LogInformation("Получены балансы всех кошельков по UserId: {UserId}. Количество: {BalancesCount}", 
                userId,
                balances.Count);

            var walletsDtos = wallets.Select(wallet => new  WalletDto
            {
                Name = wallet.Name,
                Currency = wallet.Currency,
                CurrentBalance = balances.GetValueOrDefault(wallet.Id, wallet.InitialBalance),
            }).ToList();

            return new PagedResponse<WalletDto>(
                walletsDtos,
                pageNumber,
                pageSize,
                totalCount);
        }

        /// <summary>
        /// Получает кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Кошелёк.</returns>
        public async Task<WalletDto> GetWallet(int walletId)
        {
            _logger.LogInformation("Получение кошелька по WalletId: {WalletId}", walletId);

            var wallet = await _walletRepository.GetWallet(walletId);

            _logger.LogInformation("Получен кошелёк по WalletId: {WalletId}. Название: {WalletName} Валюта: {WalletCurrency}", 
                walletId,
                wallet.Name,
                wallet.Currency);

            _logger.LogInformation("Получение баланса кошелька по WalletId: {WalletId}", walletId);

            var currentBalance = await _walletRepository.GetWalletCurrentBalance(walletId);

            _logger.LogInformation("Получен баланс кошелька по WalletId: {WalletId}", walletId);

            return new WalletDto
            {
                Name = wallet.Name,
                Currency = wallet.Currency,
                CurrentBalance = currentBalance,
            };
        }

        /// <summary>
        /// Создает кошелёк.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="createdWalletInputDto">Входной DTO кошелька.</param>
        /// <returns>Созданный кошелёк.</returns>
        public async Task<CreatedWalletOutputDto> CreateWallet(int userId, CreateWalletInputDto createdWalletInputDto)
        {
            _logger.LogInformation("Создание кошелька по UserId: {UserId}" +
            "Название: {WalletName} Валюта: {WalletCurrency}",
                userId,
                createdWalletInputDto.Name,
                createdWalletInputDto.Currency);

            var currency = createdWalletInputDto.Currency.ToUpperInvariant();

            if (currency.Length != 3 || !currency.All(char.IsLetter))
            {
                throw new ApiException(400, "Код валюты должен быть 3 заглавные буквы");
            }

            var newWallet = new Wallet
            {
                Name = createdWalletInputDto.Name,
                Currency = currency,
                InitialBalance = createdWalletInputDto.InitialBalance,
                UserId = userId,
            };

            var addWallet = await _walletRepository.AddWallet(newWallet);

            var currentBalance = await _walletRepository.GetWalletCurrentBalance(addWallet.Id);

            _logger.LogInformation("Кошелёк по UserId: {UserId} создан. " +
            "Название: {WalletName} Id кошелька: {WalletId} Валюта: {WalletCurrency}",
                userId,
                addWallet.Name,
                addWallet.Id,
                addWallet.Currency);

            return new CreatedWalletOutputDto
            {
                Id = addWallet.Id,
                Name = addWallet.Name,
                Currency = addWallet.Currency,
                InitialBalance = addWallet.InitialBalance,
                CurrentBalance = currentBalance,
            };
        }
    }
}
