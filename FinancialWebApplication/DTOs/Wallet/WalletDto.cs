namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// DTO кошелька.
    /// </summary>
    public class WalletDto
    {
        /// <summary>
        /// Название кошелька.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Валюта кошелька.
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Текущий баланс кошелька.
        /// </summary>
        public int CurrentBalance { get; set; }
    }
}
