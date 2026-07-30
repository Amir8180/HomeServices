using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// Central media-library record for every uploaded asset. Tracks the file, its
/// auto-generated thumbnail, content type, size, which logical entity it belongs
/// to and who uploaded it. Powers the admin file manager and image pickers.
/// </summary>
public class Media : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public MediaType MediaType { get; set; } = MediaType.Image;

    /// <summary>The kind of entity this media is associated with (or Unspecified for orphan library items).</summary>
    public MediaEntityType? EntityType { get; set; }

    /// <summary>Optional id of the associated entity row.</summary>
    public int? EntityId { get; set; }

    /// <summary>User id (Guid) from the Identity service who uploaded this file.</summary>
    public Guid? UploadedBy { get; set; }
}
