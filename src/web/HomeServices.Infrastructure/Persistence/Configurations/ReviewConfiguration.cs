using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews");
        b.HasKey(x => x.Id);

        b.Property(x => x.Comment).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.ExpertId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.Rating);

        b.HasOne(x => x.Order)
            .WithOne(o => o.Review)
            .HasForeignKey<Review>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Request)
            .WithOne(r => r.Review)
            .HasForeignKey<Review>(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.ToTable("SiteSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        b.Property(x => x.Value).HasMaxLength(2000);
        b.Property(x => x.Group).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> b)
    {
        b.ToTable("Media");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        b.Property(x => x.OriginalUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.UploadedBy);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Message).HasMaxLength(1000);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Url).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
