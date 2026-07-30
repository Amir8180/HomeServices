using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class MediaDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public MediaType MediaType { get; set; }
    public MediaEntityType? EntityType { get; set; }
    public int? EntityId { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
