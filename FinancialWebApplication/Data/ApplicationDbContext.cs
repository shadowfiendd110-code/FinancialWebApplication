using Microsoft.EntityFrameworkCore;
using WebApplication3.Configurations;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    /// <summary>
    /// Контекст для работы с БД.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Пользователи.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Кошельки.
        /// </summary>
        public DbSet<Wallet> Wallets { get; set; }

        /// <summary>
        /// Транзакции.
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Рефреш токены.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        /// <summary>
        /// Создание контекста.
        /// </summary>
        /// <param name="options">Настройки.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Выполняет настройку базы данных при создании контекста.
        /// </summary>
        /// <param name="modelBuilder">Билдер.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new WalletConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        }
    }
}
