using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApplication3.Models;

namespace WebApplication3.Configurations
{
    /// <summary>
    /// Конфигуратор сущности Рефреш токен.
    /// </summary>
    public class RefreshTokenConfiguration: IEntityTypeConfiguration<RefreshToken>
    {
        /// <summary>
        /// Конфигурирует сущность Рефреш токен.
        /// </summary>
        /// <param name="refreshTokenBuilder">Конфигуратор рефреш токена..</param>
        public void Configure(EntityTypeBuilder<RefreshToken> refreshTokenBuilder)
        {
            refreshTokenBuilder
                .ToTable("RefreshTokens", t => 
                {
                    t.HasCheckConstraint("CK_RefreshTokens_Expires_Future","[Expires] > [Created]");
                })
                .HasKey(rt => rt.Id);

            refreshTokenBuilder
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

            refreshTokenBuilder.HasOne(rt => rt.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            refreshTokenBuilder
            .Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(512)
                .IsUnicode(false);

            refreshTokenBuilder
            .Property(rt => rt.Created)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()")
                .ValueGeneratedOnAdd();

            refreshTokenBuilder
            .Property(rt => rt.IsRevoked)
                   .IsRequired()
                   .HasDefaultValue(false);

            refreshTokenBuilder
            .Property(rt => rt.UserId)
                   .IsRequired();
        }
    }
}
