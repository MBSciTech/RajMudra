using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajMudra.Domain.Entities;

namespace RajMudra.Infrastructure.Persistence.Configurations;

public sealed class TransactionHistoryConfiguration : IEntityTypeConfiguration<TransactionHistory>
{
    public void Configure(EntityTypeBuilder<TransactionHistory> builder)
    {
        builder.ToTable("transaction_history");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Type)
            .IsRequired()
            .HasColumnName("type");

        builder.Property(x => x.FromUserId)
            .HasColumnName("from_user_id");

        builder.Property(x => x.ToUserId)
            .HasColumnName("to_user_id");

        builder.Property(x => x.TokenId)
            .HasColumnName("token_id");

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(x => x.Purpose)
            .HasMaxLength(200)
            .HasColumnName("purpose");

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Type);
    }
}

