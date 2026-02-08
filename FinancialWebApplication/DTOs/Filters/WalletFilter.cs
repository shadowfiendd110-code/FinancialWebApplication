namespace WebApplication3.DTOs.Filters
{
    /// <summary>
    /// Фильтр для сущности Кошелёк.
    /// </summary>
    public class WalletFilter
    {
        /// <summary>
        /// Фильтр по валюте.
        /// </summary>
        public string? CurrencyEquals {  get; set; }

        /// <summary>
        /// Фильтр по названию кошелька.
        /// </summary>
        public string? NameEquals { get; set; }

        /// <summary>
        /// Фильтр по текущему балансу. 
        /// </summary>
        public int? CurrentBalance { get; set; }

        /// <summary>
        /// Имя кошелька не должно быть пустым.
        /// </summary>
        public bool HasNameEquals => !string.IsNullOrWhiteSpace(NameEquals);

        /// <summary>
        /// Валюта кошелька не должна быть пустой.
        /// </summary>
        public bool HasCurrencyEquals => !string.IsNullOrWhiteSpace(CurrencyEquals);
    }
}
