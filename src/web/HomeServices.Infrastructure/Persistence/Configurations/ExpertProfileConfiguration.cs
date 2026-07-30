using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
{
    public void Configure(EntityTypeBuilder<ExpertProfile> b)
    {
        b.ToTable("ExpertProfiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Bio).HasMaxLength(2000);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.CoverImageUrl).HasMaxLength(500);
        b.Property(x => x.ServiceArea).HasMaxLength(300);
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.BusinessHours).HasMaxLength(300);
        b.Property(x => x.RatingAverage).HasPrecision(3, 2);

        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => new { x.IsApproved, x.IsActive });
        b.HasIndex(x => x.RatingAverage);

        b.HasMany(x => x.ExpertCategories)
            .WithOne(ec => ec.ExpertProfile)
            .HasForeignKey(ec => ec.ExpertProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.ExpertServices)
            .WithOne(es => es.ExpertProfile)
            .HasForeignKey(es => es.ExpertProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.PortfolioImages)
            .WithOne(p => p.ExpertProfile)
            .HasForeignKey(p => p.ExpertProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Proposals)
            .WithOne()
            .HasForeignKey(p => p.ExpertId)
            .HasPrincipalKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExpertCategoryConfiguration : IEntityTypeConfiguration<ExpertCategory>
{
    public void Configure(EntityTypeBuilder<ExpertCategory> b)
    {
        b.ToTable("ExpertCategories");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ExpertProfileId, x.CategoryId }).IsUnique();
        b.Property(x => x.ExpertProfileId);
        b.Property(x => x.CategoryId);

        b.HasOne(x => x.ExpertProfile)
            .WithMany(e => e.ExpertCategories)
            .HasForeignKey(x => x.ExpertProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExpertServiceConfiguration : IEntityTypeConfiguration<ExpertService>
{
    public void Configure(EntityTypeBuilder<ExpertService> b)
    {
        b.ToTable("ExpertServices");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ExpertProfileId, x.ServiceId }).IsUnique();
        b.Property(x => x.CustomPrice).HasPrecision(18, 2);

        b.HasOne(x => x.ExpertProfile)
            .WithMany(e => e.ExpertServices)
            .HasForeignKey(x => x.ExpertProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Service)
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExpertPortfolioImageConfiguration : IEntityTypeConfiguration<ExpertPortfolioImage>
{
    public void Configure(EntityTypeBuilder<ExpertPortfolioImage> b)
    {
        b.ToTable("ExpertPortfolioImages");
        b.HasKey(x => x.Id);
        b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.ExpertProfileId, x.DisplayOrder });
    }
}
