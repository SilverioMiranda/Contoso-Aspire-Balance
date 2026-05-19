namespace Contoso.Data
{
    using Contoso.Data.Entities;
    using Microsoft.EntityFrameworkCore;

    public class ContosoDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Balance> Balances { get; set; }

        public ContosoDbContext(DbContextOptions<ContosoDbContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Balance>()
                .Property(b => b.Value)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Balance>()
                .Property(e => e.Date)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Balance>()
                .HasIndex(e => e.Date)
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .Property(b => b.Value)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaction>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSDATETIMEOFFSET()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Transaction>()
                .Property(e => e.RequestId)
                .HasDefaultValueSql("NEWID()")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Transaction>()
                .HasIndex(e => e.RequestId)
                .IsUnique();

            modelBuilder.Entity<Transaction>()
                .HasIndex(e => e.CreatedAt);
        }
    }
}
