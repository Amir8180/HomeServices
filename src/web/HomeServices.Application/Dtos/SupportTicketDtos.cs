using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class SupportTicketDto
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string Subject { get; set; } = string.Empty;
    public SupportTicketCategory Category { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }
    public List<SupportTicketMessageDto> Messages { get; set; } = new();
    public List<SupportTicketAttachmentDto> Attachments { get; set; } = new();
}

public class SupportTicketMessageDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Guid SenderId { get; set; }
    public bool IsFromAdmin { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SupportTicketAttachmentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? MessageId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public MediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public Guid? UploadedBy { get; set; }
}

public class CreateSupportTicketDto
{
    public int? OrderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public SupportTicketCategory Category { get; set; } = SupportTicketCategory.Other;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
    public string Description { get; set; } = string.Empty;

    // Uploads (already persisted by the caller via IFileService)
    public List<string>? FileUrls { get; set; }
    public List<string?>? ThumbnailUrls { get; set; }
    public List<MediaType>? MediaTypes { get; set; }
    public List<string>? Captions { get; set; }
}

public class SupportTicketFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public SupportTicketStatus? Status { get; set; }
    public SupportTicketCategory? Category { get; set; }
    public Guid? UserId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ReplySupportTicketDto
{
    public int TicketId { get; set; }
    public string Body { get; set; } = string.Empty;
}
