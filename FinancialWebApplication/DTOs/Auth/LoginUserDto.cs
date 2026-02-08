using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.Auth
{
    /// <summary>
    /// Логин DTO пользователя.
    /// </summary>
    public class LoginUserDto
    {
        /// <summary>
        /// Электронная почта пользователя.
        /// </summary>
        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Неверный email.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Некорректный формат email.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Пароль пользователя.
        /// </summary>
        [Required(ErrorMessage = "Пароль обязателен.")]
        [DataType(DataType.Password, ErrorMessage = "Неверный пароль.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public LoginUserDto() { }
    }
}
