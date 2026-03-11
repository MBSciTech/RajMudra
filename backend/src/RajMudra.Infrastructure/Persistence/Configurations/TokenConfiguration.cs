using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajMudra.Domain.Entities;

namespace RajMudra.Infrastructure.Persistence.Configurations;

public sealed class TokenConfiguration : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.ToTable("tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.OwnerId)
            .IsRequired()
            .HasColumnName("owner_id");

        builder.Property(x => x.Denomination)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasColumnName("denomination");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(x => x.IsSpent)
            .IsRequired()
            .HasColumnName("is_spent");

        builder.Property(x => x.Purpose)
            .HasMaxLength(200)
            .HasColumnName("purpose");

        builder.HasIndex(x => new { x.OwnerId, x.IsSpent });
        builder.HasIndex(x => x.CreatedAt);
    }
}

