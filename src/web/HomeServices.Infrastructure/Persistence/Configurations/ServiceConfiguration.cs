using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Services");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.IconUrl).HasMaxLength(500);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.BasePrice).HasPrecision(18, 2);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.DisplayOrder });

        b.HasMany(x => x.Images)
            .WithOne(i => i.Service)
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Requests)
            .WithOne(r => r.Service!)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ServiceImageConfiguration : IEntityTypeConfiguration<ServiceImage>
{
    public void Configure(EntityTypeBuilder<ServiceImage> b)
    {
        b.ToTable("ServiceImages");
        b.HasKey(x => x.Id);
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.AltText).HasMaxLength(300);
        b.HasIndex(x => new { x.ServiceId, x.DisplayOrder });
    }
}
