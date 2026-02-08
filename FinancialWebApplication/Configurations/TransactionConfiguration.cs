using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Configurations
{
    /// <summary>
    /// Конфигуратор сущности Транзакция.
    /// </summary>
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        /// <summary>
        /// Конфигурирует сущность Транзакция.
        /// </summary>
        /// <param name="transactionBuilder">Конфигуратор кошелька.</param>
        public void Configure(EntityTypeBuilder<Transaction> transactionBuilder)
        {
            transactionBuilder
                .ToTable("Transactions")
                .HasKey(t => t.Id);

            transactionBuilder
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId);

            transactionBuilder
            .Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(100);

            transactionBuilder
            .Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(10);

            transactionBuilder
                .HasIndex(t => t.Date);

            transactionBuilder
                .HasIndex(t => new { t.WalletId, t.Date });

            transactionBuilder
                .HasIndex(t => new { t.Type, t.Date });

            transactionBuilder
                .HasIndex(t => t.Type);
        }
    }
}
