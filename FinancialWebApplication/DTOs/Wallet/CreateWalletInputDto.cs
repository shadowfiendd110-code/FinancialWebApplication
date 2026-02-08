using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// Входной DTO при создании кошелька.
    /// </summary>
    public class CreateWalletInputDto
    {
        /// <summary>
        /// Название кошелька.
        /// </summary>
        [Required(ErrorMessage = "Название кошелька обязательно.")]
        [StringLength(100, ErrorMessage = "Название кошелька слишком длинное.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Валюта кошелька.
        /// </summary>
        [Required(ErrorMessage = "Валюта кошелька обязательна.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Код валюты должен быть 3 символа")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Неверный формат валюты. Используйте 3 заглавные буквы, например: USD, EUR, RUB")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Начальный баланс кошелька.
        /// </summary>
        [Required(ErrorMessage = "Начальный баланс кошелька обязателен.")]
        [Range(0, 10000000, ErrorMessage = "Не верный начальный баланс кошелька.")]
        public int InitialBalance { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public CreateWalletInputDto() { }
    }
}