using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class PaymentVerificationReportConfiguration : IEntityTypeConfiguration<PaymentVerificationReport>
{
    public void Configure(EntityTypeBuilder<PaymentVerificationReport> b)
    {
        b.ToTable("PaymentVerificationReports");
        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.SenderFullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.BankRefNumber).HasMaxLength(100);
        b.Property(x => x.CustomerNote).HasMaxLength(2000);
        b.Property(x => x.SupportNote).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.CreatedAt);

        b.HasOne(x => x.Order)
            .WithMany(o => o.PaymentReports)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class WorkCompletionReportConfiguration : IEntityTypeConfiguration<WorkCompletionReport>
{
    public void Configure(EntityTypeBuilder<WorkCompletionReport> b)
    {
        b.ToTable("WorkCompletionReports");
        b.HasKey(x => x.Id);

        b.Property(x => x.ExpertNote).HasMaxLength(4000);
        b.Property(x => x.CustomerNote).HasMaxLength(4000);
        b.Property(x => x.SupportNote).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.ExpertId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.CreatedAt);

        b.HasOne(x => x.Order)
            .WithMany(o => o.CompletionReports)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Attachments)
            .WithOne(a => a.Report)
            .HasForeignKey(a => a.WorkCompletionReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkCompletionAttachmentConfiguration : IEntityTypeConfiguration<WorkCompletionAttachment>
{
    public void Configure(EntityTypeBuilder<WorkCompletionAttachment> b)
    {
        b.ToTable("WorkCompletionAttachments");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Uploader).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.WorkCompletionReportId);
    }
}

public class ExpertPayoutConfiguration : IEntityTypeConfiguration<ExpertPayout>
{
    public void Configure(EntityTypeBuilder<ExpertPayout> b)
    {
        b.ToTable("ExpertPayouts");
        b.HasKey(x => x.Id);

        b.Property(x => x.PayoutNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.GrossAmount).HasPrecision(18, 2);
        b.Property(x => x.CommissionPercent).HasPrecision(5, 2);
        b.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        b.Property(x => x.NetAmount).HasPrecision(18, 2);
        b.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.ServiceTitle).HasMaxLength(300).IsRequired();

        b.HasIndex(x => x.PayoutNumber).IsUnique();
        b.HasIndex(x => x.ExpertId);
        b.HasIndex(x => x.PaidAt);
        b.HasIndex(x => x.OrderId).IsUnique();

        b.HasOne(x => x.Order)
            .WithOne(o => o.Payout)
            .HasForeignKey<ExpertPayout>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
