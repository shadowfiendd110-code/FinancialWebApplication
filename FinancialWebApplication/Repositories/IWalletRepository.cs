using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.DTOs.Filters;
using WebApplication3.Exceptions;
using WebApplication3.Models;

namespace WebApplication3.Repositories
{
    /// <summary>
    /// Репозиторий для управления кошельками.
    /// </summary>
    public interface IWalletRepository
    {
        /// <summary>
        /// Добавляет кошелёк в БД.
        /// </summary>
        /// <param name="wallet">Кошелёк.</param>
        /// <returns>Кошелёк.</returns>
        Task<Wallet> AddWallet(Wallet wallet);

        /// <summary>
        /// Получает кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Кошелёк.</returns>
        Task <Wallet> GetWallet(int walletId);

        /// <summary>
        /// Получает все кошельки пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для кошельков.</param>
        /// <returns>Все кошельки пользователя.</returns>
        Task<(List<Wallet>, int)> GetAllUserWallets(int userId, WalletFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Обновляет кошелёк.
        /// </summary>
        /// <param name="wallet">Кошелёк.</param>
        /// <returns>Обновлённый кошелёк.</returns>
        Task UpdateWallet(Wallet wallet);

        /// <summary>
        /// Удаляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        Task DeleteWallet(int walletId);

        /// <summary>
        /// Получает текущий баланс кошелька.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Текущий баланс кошелька.</returns>
        Task<int> GetWalletCurrentBalance(int walletId);

        /// <summary>
        /// Получает текущий баланс для всех кошельков пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>Текущий баланс для всех кошельков пользователя.</returns>
        Task<Dictionary<int, int>> GetAllWalletsCurrentBalance(int userId);

        /// <summary>
        /// Получает расход и доход для кошелька за месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Расход и доход для кошелька за месяц.</returns>
        Task<(string Name, string Currency, int Income, int Expense)> GetWalletMonthlySummary(int walletId, int year, int month);

        /// <summary>
        /// Получает топ 3 траты за месяц для кошелька.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Топ 3 траты за месяц для кошелька.</returns>
        public Task<Dictionary<int, List<Transaction>>> GetTopThreeExpensesPerWallet(int userId, int year, int month);
    }

    /// <summary>
    /// Репозиторий для управления кошельками.
    /// </summary>
    public class WalletRepository: IWalletRepository
    {
        /// <summary>
        /// Контекст для работы с БД.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Создание репозитория.
        /// </summary>
        /// <param name="context">Контекст для работы с бд.</param>
        public WalletRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает топ 3 траты для кошелька за месяц.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Топ 3 траты для кошелька за месяц.</returns>
        public async Task<Dictionary<int, List<Transaction>>> GetTopThreeExpensesPerWallet(
            int userId, 
            int year, 
            int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var walletIds = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => w.Id)
                .ToListAsync();

            var result = new Dictionary<int, List<Transaction>>();

            foreach (var walletId in walletIds)
            {
                var expenses = await _context.Transactions
                    .Where(t => t.WalletId == walletId &&
                               t.Type == TransactionType.Expense &&
                               t.Date >= startDate &&
                               t.Date < endDate)
                    .OrderByDescending(t => t.TransactionSum)
                    .Take(3)
                    .AsNoTracking()
                    .ToListAsync();

                if (expenses.Count > 0)
                {
                    result[walletId] = expenses;
                }
            }

            return result;
        }

        /// <summary>
        /// Получает доходы и расходы кошелька за месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>доходы и расходы для кошелька за месяц.</returns>
        public async Task<(string Name, string Currency, int Income, int Expense)>GetWalletMonthlySummary(
            int walletId, 
            int year, 
            int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var queryResult = await (
                from w in _context.Wallets
                where w.Id == walletId
                select new
                {
                    w.Name,
                    w.Currency,
                    Income = w.Transactions
                        .Where(t => t.Type == TransactionType.Income &&
                                   t.Date >= startDate &&
                                   t.Date < endDate)
                        .Sum(t => (int?)t.TransactionSum) ?? 0,
                    Expense = w.Transactions
                        .Where(t => t.Type == TransactionType.Expense &&
                                   t.Date >= startDate &&
                                   t.Date < endDate)
                        .Sum(t => (int?)t.TransactionSum) ?? 0
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (queryResult == null)
            {
                throw new WalletNotFoundException($"Кошелек с ID {walletId} не найден.");
            }

            return (queryResult.Name, queryResult.Currency,
                    queryResult.Income, queryResult.Expense);
        }

        /// <summary>
        /// Получает текущий баланс всех кошельков пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <returns>Текущий баланс для всех кошельков пользователя.</returns>
        public async Task<Dictionary<int, int>> GetAllWalletsCurrentBalance(int userId)
        {
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .AsNoTracking()
                .Select(w => new { w.InitialBalance, w.Id })
                .ToListAsync();

            var walletsIds = wallets
                .Select(w => w.Id)
                .ToList();

            var incomes = await GetWalletsTransactionsSummary(walletsIds, TransactionType.Income);
            var expenses = await GetWalletsTransactionsSummary(walletsIds, TransactionType.Expense);

            var result = new Dictionary<int, int>();

            foreach (var wallet in wallets)
            {
                var income = incomes.GetValueOrDefault(wallet.Id, 0);
                var expense = expenses.GetValueOrDefault(wallet.Id, 0);

                result[wallet.Id] = wallet.InitialBalance + income - expense;
            }

            return result;
        }

        /// <summary>
        /// Получает список транзакций типа доход/расход (в зависимости от переданного типа транзакции) кошелька.
        /// </summary>
        /// <param name="walletsIds">Id кошельков.</param>
        /// <param name="type">Тип транзакций.</param>
        /// <returns>Список транзакций типа доход/расход (в зависимости от переданного типа транзакции) кошелька.</returns>
        private async Task<Dictionary<int, int>> GetWalletsTransactionsSummary(List<int> walletsIds, TransactionType type)
        {
            return await _context.Transactions
                        .Where(t => walletsIds.Contains(t.WalletId) && t.Type == type)
                        .GroupBy(t => t.WalletId)
                        .Select(g => new { WalletId = g.Key, Total = g.Sum(t => t.TransactionSum) })
                        .ToDictionaryAsync(x => x.WalletId, x => x.Total);
        }

        /// <summary>
        /// Получает текущий баланс кошелька.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Текущий баланс кошелька.</returns>
        public async Task<int> GetWalletCurrentBalance(int walletId)
        {
            var walletExists = await _context.Wallets.AnyAsync(w => w.Id == walletId);
            if (!walletExists)
            {
                throw new WalletNotFoundException();
            }

            var income = await _context.Transactions
                .Where(t => t.WalletId == walletId && t.Type == TransactionType.Income)
                .SumAsync(t => (int?)t.TransactionSum) ?? 0;

            var expense = await _context.Transactions
                .Where(t => t.WalletId == walletId && t.Type == TransactionType.Expense)
                .SumAsync(t => (int?)t.TransactionSum) ?? 0;

            var wallet = await _context.Wallets.FindAsync(walletId);

            return wallet!.InitialBalance + income - expense;
        }

        /// <summary>
        /// Удаляет кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелёк.</param>
        public async Task DeleteWallet(int walletId)
        {
            var wallet = await GetWallet(walletId);

            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновляет кошелёк.
        /// </summary>
        /// <param name="wallet">Кошелёк.</param>
        /// <returns>Обновлённый кошелёк.</returns>
        public async Task UpdateWallet(Wallet wallet)
        {
            _context.Wallets.Update(wallet);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получает все кошельки пользователя.
        /// </summary>
        /// <param name="userId">Id пользователя.</param>
        /// <param name="pageNumber">Текущий номер страницы.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="filter">Фильтр для кошельков.</param>
        /// <returns>Все кошельки пользователя.</returns>
        public async Task<(List<Wallet>, int)> GetAllUserWallets(
        int userId, WalletFilter? filter = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Wallets
                .Where(w => w.UserId == userId)
                .AsNoTracking();

            if(filter != null)
            {
                if(filter.HasCurrencyEquals)
                {
                    query = query.Where(w => w.Currency == filter.CurrencyEquals);
                }

                if(filter.HasNameEquals)
                {
                    query = query.Where(w => w.Name == filter.NameEquals);
                }
            }

            var count = await query.CountAsync();

            if (count == 0)
            {
                return (new List<Wallet>(), 0);
            }

            var skip = (pageNumber - 1) * pageSize;

            var wallets = await query
                .Include(w => w.Transactions)
                .OrderByDescending(w => w.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (wallets, count);
        }

        /// <summary>
        /// Добавляет кошелёк в БД.
        /// </summary>
        /// <param name="wallet">Кошелёк.</param>
        /// <returns>Кошелёк.</returns>
        public async Task<Wallet> AddWallet(Wallet wallet)
        {
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        /// <summary>
        /// Получает кошелёк.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <returns>Кошелёк.</returns>
        public async Task<Wallet> GetWallet(int  walletId)
        {

            var wallet = await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId);

            if(wallet == null)
            {
                throw new WalletNotFoundException();
            }

            return wallet;
        }
    }
}
