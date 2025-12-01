using Microsoft.EntityFrameworkCore;
using Calculator.Models;

namespace Calculator.Data;

public class CalculatorContext : DbContext
{
    public CalculatorContext(DbContextOptions<CalculatorContext> options) : base(options)
    {
    }

    public DbSet<CalculationHistory> CalculationHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalculationHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }
}