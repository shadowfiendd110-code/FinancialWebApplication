using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.Auth
{
    /// <summary>
    /// Входное регистрационное DTO пользователя.
    /// </summary>
    public class RegisterUserDto
    {
        /// <summary>
        /// Имя пользователя.
        /// </summary>
        [Required(ErrorMessage = "Имя обязательно.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 100 символов.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Электронная почта пользователя.
        /// </summary>
        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный формат email.")]
        [StringLength(256, ErrorMessage = "Email слишком длинный.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Некорректный формат email.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Пароль пользователя.
        /// </summary>
        [Required(ErrorMessage = "Пароль обязателен.")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов в длину.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Подтверждение пароля.
        /// </summary>
        [Required(ErrorMessage = "Подтвердите пароль.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public RegisterUserDto() { }
    }
}
