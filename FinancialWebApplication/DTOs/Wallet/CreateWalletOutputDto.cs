namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// Выходной DTO при создании кошелька.
    /// </summary>
    public class CreatedWalletOutputDto
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
        /// Начальный баланс кошелька.
        /// </summary>
        public int InitialBalance { get; set; }

        /// <summary>
        /// Текущий баланс кошелька.
        /// </summary>
        public int CurrentBalance { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CreatedWalletOutputDto() { }
    }
}
