namespace WebApplication3.Models
{
    /// <summary>
    /// Транзакция.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Id транзакции.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Описание транзакции.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Дата транзакции.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Сумма транзакции.
        /// </summary>
        public int TransactionSum { get; set; }
        
        /// <summary>
        /// Тип транзакции.
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Кошелёк, в котором произошла транзакция.
        /// </summary>
        public Wallet Wallet { get; set; }

        /// <summary>
        /// Id кошелька.
        /// </summary>
        public int WalletId { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public Transaction() { }

        /// <summary>
        /// Создание кошелька.
        /// </summary>
        /// <param name="transactionDescription">Описание транзакции.</param>
        /// <param name="transactionDate">Дата транзакции.</param>
        /// <param name="transactionSum">Сумма транзакции.</param>
        /// <param name="transactionType">Тип транзакции.</param>
        public Transaction(string transactionDescription, DateTime transactionDate,
        int transactionSum, TransactionType transactionType)
        {
            Description = transactionDescription;
            Date = transactionDate;
            TransactionSum = transactionSum;
            Type = transactionType;
        }
    }
}
