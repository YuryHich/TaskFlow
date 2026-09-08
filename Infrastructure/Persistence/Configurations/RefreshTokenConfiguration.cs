using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(500);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => token.UserId);
    }
}
