namespace WebApplication3.DTOs.Auth
{
    /// <summary>
    /// Входной DTO рефреш токена.
    /// </summary>
    public class RefreshTokenDto
    {
        /// <summary>
        /// Рефреш токен.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public RefreshTokenDto() { }
    }
}
