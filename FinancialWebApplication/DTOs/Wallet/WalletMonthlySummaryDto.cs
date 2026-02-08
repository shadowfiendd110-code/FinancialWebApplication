namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// DTO анализа кошелька за месяц.
    /// </summary>
    public class WalletMonthlySummaryDto
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
        /// Доход за месяц.
        /// </summary>
        public int Income { get; set; }

        /// <summary>
        /// Расход за месяц.
        /// </summary>
        public int Expense { get; set; }

        /// <summary>
        /// Год.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Месяц.
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public WalletMonthlySummaryDto() { }
    }
}
