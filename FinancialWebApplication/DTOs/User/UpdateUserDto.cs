using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs.User
{
    /// <summary>
    /// DTO обновления пользователя.
    /// </summary>
    public class UpdateUserDto
    {
        /// <summary>
        /// Id пользователя.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Id обязателен для обновления.")]
        public int Id { get; set; }

        /// <summary>
        /// Имя пользователя.
        /// </summary>
        [Required(ErrorMessage = "Имя обязательно.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 100 символов.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// E-mail пользователя.
        /// </summary>
        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный формат email.")]
        [StringLength(256, ErrorMessage = "Email слишком длинный.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ErrorMessage = "Некорректный формат email.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public UpdateUserDto() { }
    }
}
