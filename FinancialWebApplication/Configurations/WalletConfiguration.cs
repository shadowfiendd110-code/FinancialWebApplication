using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Configurations
{
    /// <summary>
    /// Конфигуратор сущности Кошелёк.
    /// </summary>
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        /// <summary>
        /// Конфигурирует сущность Кошелёк.
        /// </summary>
        /// <param name="walletBuilder">Конфигуратор кошелька.</param>
        public void Configure(EntityTypeBuilder<Wallet> walletBuilder)
        {
            walletBuilder
                .ToTable("Wallets", t => 
                {
                    t.HasCheckConstraint("CK_Wallets_InitialBalance","[InitialBalance] >= 0");
                })
                .HasKey(w => w.Id);
            
            walletBuilder
            .HasOne(w => w.User)
            .WithMany(u => u.Wallets)
            .HasForeignKey(u => u.UserId);

            walletBuilder
            .Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(100);

            walletBuilder
            .Property(w => w.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .IsFixedLength(true)
                .HasConversion<string>();

            walletBuilder
            .Property(w => w.InitialBalance)
                .IsRequired()
                .HasColumnType("int");

            walletBuilder
                .HasIndex(w => w.UserId);
        }
    }
}
