using WebApplication3.Models;

namespace WebApplication3.DTOs.Filters
{
    /// <summary>
    /// Фильтр для сущности "Транзакция".
    /// </summary>
    public class TransactionFilter
    {
        /// <summary>
        /// Фильтр по типу транзакции.
        /// </summary>
        public TransactionType? TransactionType { get; set; }

        /// <summary>
        /// Фильтр по описанию.
        /// </summary>
        public string? DescriptionContains { get; set; }

        /// <summary>
        /// Фильтр по минимальной сумме транзакции.
        /// </summary>
        public int? MinAmount { get; set; }

        /// <summary>
        /// Фильтр по максимальной сумме транзакции.
        /// </summary>
        public int? MaxAmount { get; set; }

        /// <summary>
        /// Фильтра по начальной дате транзакций.
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Фильтра по конечной дате транзакций.
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Тип транзакции не должен быть равен null.
        /// </summary>
        public bool HasTypeFilter => TransactionType != null;

        /// <summary>
        /// Описание транзакции не должно быть пустым или равно null.
        /// </summary>
        public bool HasDescriptionContains => !string.IsNullOrWhiteSpace(DescriptionContains);

        /// <summary>
        /// Сумма транзакции не должна быть равна null.
        /// </summary>
        public bool HasAmountFilter => MaxAmount.HasValue || MinAmount.HasValue;

        /// <summary>
        /// Конечная дата транзакции не должна быть равна null.
        /// </summary>
        public bool HasDateToFilter => DateTo.HasValue;

        /// <summary>
        /// Начальная дата транзакции не должна быть равна null.
        /// </summary>
        public bool HasDateFromFilter => DateFrom.HasValue;
    }
}
