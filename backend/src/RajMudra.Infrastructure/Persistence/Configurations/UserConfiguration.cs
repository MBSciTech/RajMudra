using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RajMudra.Domain.Entities;

namespace RajMudra.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("email");

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasColumnName("password_hash");

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("role");

        builder.Property(x => x.MerchantCategory)
            .HasMaxLength(200)
            .HasColumnName("merchant_category");
    }
}

