using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.User
{
    /// <summary>
    /// DTO для смены пароля пользователя.
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// Старый пароль.
        /// </summary>
        [Required(ErrorMessage = "Старый пароль обязателен.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Новый пароль.
        /// </summary>
        [Required(ErrorMessage = "Новый пароль обязателен.")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов в длину.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Подтверждение пароля.
        /// </summary>
        [Required(ErrorMessage = "Подтвердите новый пароль.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public ChangePasswordDto() { }
    }
}
