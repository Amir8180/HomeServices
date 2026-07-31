using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> b)
    {
        b.ToTable("ServiceRequests");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.ZipCode).HasMaxLength(20);
        b.Property(x => x.Urgency).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.BudgetMin).HasPrecision(18, 2);
        b.Property(x => x.BudgetMax).HasPrecision(18, 2);

        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => new { x.CategoryId, x.Status });
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAt);

        b.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Service)
            .WithMany(s => s.Requests)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(x => x.Proposals)
            .WithOne(p => p.Request)
            .HasForeignKey(p => p.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Images)
            .WithOne(i => i.Request)
            .HasForeignKey(i => i.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.AcceptedProposal)
            .WithMany()
            .HasForeignKey(x => x.AcceptedProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Order)
            .WithOne(o => o.Request)
            .HasForeignKey<Order>(o => o.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Review)
            .WithOne(r => r.Request)
            .HasForeignKey<Review>(r => r.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequestImageConfiguration : IEntityTypeConfiguration<RequestImage>
{
    public void Configure(EntityTypeBuilder<RequestImage> b)
    {
        b.ToTable("RequestImages");
        b.HasKey(x => x.Id);
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.AltText).HasMaxLength(300);
        b.HasIndex(x => new { x.RequestId, x.DisplayOrder });
    }
}
