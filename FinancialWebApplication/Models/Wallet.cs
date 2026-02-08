namespace WebApplication3.Models
{
    /// <summary>
    /// Кошелёк.
    /// </summary>
    public class Wallet
    {
        /// <summary>
        /// Id кошелька.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя кошелька.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Валюта кошелька.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Начальный баланс кошелька.
        /// </summary>
        public int InitialBalance { get; set; }

        /// <summary>
        /// Владелец кошелька.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Текущий баланс кошелька.
        /// </summary>
        public int CurrentBalance { get; }

        /// <summary>
        /// Id владельца кошелька.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Транзакции кошелька.
        /// </summary>
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public Wallet() { }

        /// <summary>
        /// Создание кошелька.
        /// </summary>
        /// <param name="name">Название кошелька.</param>
        /// <param name="currency">Валюта кошелька.</param>
        /// <param name="initialBalance">Начальный баланс кошелька.</param>
        public Wallet(string name, string currency, int initialBalance)
        {
            Name = name;
            Currency = currency;
            InitialBalance = initialBalance;
        }
    }
}
