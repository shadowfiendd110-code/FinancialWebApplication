using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.Wallet
{
    /// <summary>
    /// DTO обновленного кошелька.
    /// </summary>
    public class UpdateWalletDto
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
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Неверный формат валюты. Пример: USD, EUR, RUB")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public UpdateWalletDto() { }
    }
}
