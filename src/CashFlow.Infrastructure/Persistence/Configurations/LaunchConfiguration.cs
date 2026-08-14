using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Infrastructure.Persistence.Configurations;

public sealed class LaunchConfiguration : IEntityTypeConfiguration<Launch>
{
    public void Configure(EntityTypeBuilder<Launch> builder)
    {
        builder.ToTable("launches");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(l => l.Amount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.Property(l => l.OccurredOn)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => l.OccurredOn)
            .HasDatabaseName("ix_launches_occurred_on");
    }
}
