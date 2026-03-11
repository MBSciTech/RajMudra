using Microsoft.EntityFrameworkCore;
using RajMudra.Domain.Entities;

namespace RajMudra.Infrastructure.Persistence;

public sealed class RajMudraDbContext : DbContext
{
    public RajMudraDbContext(DbContextOptions<RajMudraDbContext> options) : base(options)
    {
    }

    public DbSet<Token> Tokens => Set<Token>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TransactionHistory> TransactionHistory => Set<TransactionHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RajMudraDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

