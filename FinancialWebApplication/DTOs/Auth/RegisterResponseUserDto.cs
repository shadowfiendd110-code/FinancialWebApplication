namespace WebApplication3.DTOs.Auth
{
    /// <summary>
    /// Выходное регистрационное DTO пользователя.
    /// </summary>
    public class RegisterResponseUserDto
    {
        /// <summary>
        /// Id пользователя.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Электронная почта пользователя.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Роль пользователя.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// JWT токен пользователя.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Время жизни токена пользователя.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Рефреш токен.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public RegisterResponseUserDto() { }
    }
}
