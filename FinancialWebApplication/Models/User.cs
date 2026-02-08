namespace WebApplication3.Models
{
    /// <summary>
    /// Пользователь.
    /// </summary>
    public class User
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
        /// E-mail пользователя.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hash пароля пользователя.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Роль пользователя.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Кошельки пользователя.
        /// </summary>
        public List<Wallet> Wallets { get; set; } = new List<Wallet>();

        /// <summary>
        /// Рефреш токены пользователя.
        /// </summary>
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public User() {}

        /// <summary>
        /// Создание пользователя.
        /// </summary>
        /// <param name="name">Имя пользователя.</param>
        /// <param name="email">E-mail пользователя.</param>
        /// <param name="role">Роль пользователя.</param>
        public User(string name, string email, string role)
        {
            Name = name;
            Email = email;
            Role = role;
        }
    }
}
