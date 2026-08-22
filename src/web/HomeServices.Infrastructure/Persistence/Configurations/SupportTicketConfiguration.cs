using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> b)
    {
        b.ToTable("SupportTickets");
        b.HasKey(x => x.Id);

        b.Property(x => x.TicketNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.TicketNumber).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.LastActivityAt);

        b.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(x => x.Messages)
            .WithOne(m => m.Ticket)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Attachments)
            .WithOne(a => a.Ticket)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> b)
    {
        b.ToTable("SupportTicketMessages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => x.TicketId);
    }
}

public class SupportTicketAttachmentConfiguration : IEntityTypeConfiguration<SupportTicketAttachment>
{
    public void Configure(EntityTypeBuilder<SupportTicketAttachment> b)
    {
        b.ToTable("SupportTicketAttachments");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.Caption).HasMaxLength(500);
        b.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.TicketId);
    }
}
