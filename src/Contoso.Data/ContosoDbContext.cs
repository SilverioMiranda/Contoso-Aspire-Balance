namespace Contoso.Data
{
    using Microsoft.EntityFrameworkCore;
    using Contoso.Data.Entities;

    public class ContosoDbContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Balance> Balances { get; set; }
        public ContosoDbContext(DbContextOptions<ContosoDbContext> dbContextOptions) : base(dbContextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Balance>()
                .Property(b => b.Value)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Balance>()
        .Property(e => e.Date)
        .HasDefaultValueSql("SYSDATETIMEOFFSET()");
            modelBuilder.Entity<Transaction>()
                .Property(b => b.Value)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaction>()
       .Property(e => e.CreatedAt)
       .HasDefaultValueSql("SYSDATETIMEOFFSET()");
        }
    }
}
