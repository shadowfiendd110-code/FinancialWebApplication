namespace WebApplication3.Models
{
    /// <summary>
    /// Рефреш токен.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Id рефреш токена.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Токен.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Время жизни токена.
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        /// Дата создания токена.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Возвращает true, если токен отозван.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Возвращает true, если срок действия токена истёк.
        /// </summary>
        public bool IsExpired => Expires <= DateTime.UtcNow;

        /// <summary>
        /// Возвращает true, если токен активен.
        /// </summary>
        public bool IsActive => !IsExpired;

        /// <summary>
        /// Id владельца токена.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Владелец токена.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public RefreshToken() { }
    }
}
