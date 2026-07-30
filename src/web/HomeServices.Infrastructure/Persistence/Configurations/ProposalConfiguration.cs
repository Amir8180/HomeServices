using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> b)
    {
        b.ToTable("Proposals");
        b.HasKey(x => x.Id);

        b.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.Message).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.RequestId, x.Status });
        b.HasIndex(x => x.ExpertId);

        b.HasOne(x => x.Request)
            .WithMany(r => r.Proposals)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Order)
            .WithOne(o => o.Proposal)
            .HasForeignKey<Order>(o => o.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();

        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.ExpertId);
        b.HasIndex(x => x.Status);

        b.HasOne(x => x.Request)
            .WithOne(r => r.Order)
            .HasForeignKey<Order>(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Proposal)
            .WithOne(p => p.Order)
            .HasForeignKey<Order>(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Review)
            .WithOne(r => r.Order)
            .HasForeignKey<Review>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.TransactionId).HasMaxLength(100);
        b.Property(x => x.GatewayReference).HasMaxLength(200);
        b.HasIndex(x => x.OrderId);
    }
}
