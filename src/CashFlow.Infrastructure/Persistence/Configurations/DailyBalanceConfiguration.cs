using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

public sealed class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    public void Configure(EntityTypeBuilder<DailyBalance> builder)
    {
        builder.ToTable("daily_balances");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.ReferenceDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.TotalCredits)
            .HasColumnName("total_credits")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(b => b.TotalDebits)
            .HasColumnName("total_debits")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(b => b.ClosingBalance)
            .HasColumnName("closing_balance")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.ConsolidatedAtUtc);

        builder.Property(b => b.FailedAttempts)
            .IsRequired();

        builder.Property(b => b.FailureReason)
            .HasMaxLength(1000);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        // Garante consolidação idempotente: nunca duas linhas para a mesma data.
        builder.HasIndex(b => b.ReferenceDate)
            .IsUnique()
            .HasDatabaseName("ux_daily_balances_reference_date");
    }
}
