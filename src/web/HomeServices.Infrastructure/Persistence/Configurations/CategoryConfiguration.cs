using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.IconUrl).HasMaxLength(500);
        b.Property(x => x.Group).HasConversion<string>().HasMaxLength(30).IsRequired();

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.ParentCategoryId, x.DisplayOrder });

        // Self-referencing parent for sub-categories
        b.HasOne(x => x.Parent)
            .WithMany(p => p.SubCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Services)
            .WithOne(s => s.Category)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
