using WebApplication3.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Exceptions;
using WebApplication3.DTOs.Filters;
using WebApplication3.Data;

namespace WebApplication3.Repositories
{
    /// <summary>
    /// Репозиторий для работы с Транзакциями.
    /// </summary>
    public interface ITransactionRepository
    {
        /// <summary>
        /// Добавляет транзакцию в БД.
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <returns>Транзакцию.</returns>
        Task<Transaction> AddTransaction (Transaction transaction);

        /// <summary>
        /// Получает транзакцию.
        /// </summary>
        /// <param name="transactionId">Id транзакции</param>
        /// <returns>Транзакцию.</returns>
        Task<Transaction> GetTransaction(int transactionId);

        /// <summary>
        /// Получает все транзакции кошелька.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="filter">Фильтр транзакций.</param>
        /// <param name="pageNumber">Номер текущей страницы.</param>
        /// <param name="pageSize">Размер страницы (в записях).</param>
        Task<(List<Transaction>, int)> GetAllWalletTransactions(
            int walletId, TransactionFilter? filter = null, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Получает сгруппированные и отсортированные транзакции за конкретный месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Сгруппированные и отсортированные транзакции за конкретный месяц.</returns>
        Task<List<Transaction>> GetGroupAndSortTransactions(int walletId, int year, int month);
    }

    /// <summary>
    /// Репозиторий для работы с Транзакциями.
    /// </summary>
    public class TransactionRepository: ITransactionRepository
    {
        /// <summary>
        /// Контекст для работы с БД.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Создание репозитория.
        /// </summary>
        /// <param name="context">Контекст для работы с БД.</param>
        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает сгруппированные и отсортированные транзакции за конкретный месяц.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="year">Год.</param>
        /// <param name="month">Месяц.</param>
        /// <returns>Сгруппированные и отсортированные транзакции за конкретный месяц.</returns>
        public async Task<List<Transaction>> GetGroupAndSortTransactions(
            int walletId, 
            int year, 
            int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var transactions = await _context.Transactions
                .Where(t => t.WalletId == walletId &&
                           t.Date >= startDate &&
                           t.Date < endDate)
                .AsNoTracking()
                .ToListAsync();

            return transactions
                .GroupBy(t => t.Type)
                .OrderByDescending(g => g.Sum(t => t.TransactionSum))
                .SelectMany(g => g.OrderByDescending(t => t.TransactionSum)
                                 .ThenBy(t => t.Date))
                .ToList();
        }

        /// <summary>
        /// Получает все транзакции кошелька.
        /// </summary>
        /// <param name="walletId">Id кошелька.</param>
        /// <param name="filter">Фильтр транзакций.</param>
        /// <param name="pageNumber">Номер текущей страницы.</param>
        /// <param name="pageSize">Размер страницы (в записях).</param>
        /// <returns>Все транзакции кошелька.</returns>
        public async Task<(List<Transaction>, int)> GetAllWalletTransactions(
            int walletId, 
            TransactionFilter? filter = null, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            var query = _context.Transactions
                .Where(t => t.WalletId == walletId)
                .AsNoTracking();

            if(filter != null)
            {
                if(filter.HasTypeFilter)
                {
                    query = query.Where(t => t.Type == filter.TransactionType);
                }

                if(filter.HasDateFromFilter)
                {
                    query = query.Where(t => t.Date >= filter.DateFrom.Value);
                }

                if(filter.HasDateToFilter)
                {
                    query = query.Where(t => t.Date < filter.DateTo.Value.AddDays(1));
                }

                if(filter.MaxAmount.HasValue)
                {
                    query = query.Where(t => t.TransactionSum <= filter.MaxAmount.Value);
                }

                if(filter.MinAmount.HasValue)
                {
                    query = query.Where(t => t.TransactionSum >= filter.MinAmount.Value);
                }

                if(filter.HasDescriptionContains)
                {
                    query = query.Where(t => EF.Functions.Like(t.Description, $"%{filter.DescriptionContains}%"));
                }
            }

            var count = await query.CountAsync();

            var skip = (pageNumber - 1) * pageSize;

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (transactions, count);
        }

        /// <summary>
        /// Получает транзакцию.
        /// </summary>
        /// <param name="transactionId">Id транзакции</param>
        /// <returns>Транзакцию.</returns>
        public async Task<Transaction> GetTransaction(int transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);

            if (transaction == null)
            {
                throw new TransactionNotFoundException();
            }

            return transaction;
        }

        /// <summary>
        /// Добавялет транзакцию в БД.
        /// </summary>
        /// <param name="transaction">Транзакция.</param>
        /// <returns>Транзакцию.</returns>
        public async Task<Transaction> AddTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}
