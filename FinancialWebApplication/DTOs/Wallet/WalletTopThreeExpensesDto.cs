using WebApplication3.DTOs.Transaction;

namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// DTO топ 3 трат за месяц.
    /// </summary>
    public class WalletTopExpensesDto
    {
        /// <summary>
        /// Id кошелька.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название кошелька.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Валюта кошелька.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Список топ 3 трат за месяц.
        /// </summary>
        public List<TransactionDto> TopThreeExpenses { get; set; } = new List<TransactionDto>();

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public WalletTopExpensesDto() { }
    }
}
