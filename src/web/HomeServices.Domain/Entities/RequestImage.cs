using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A photo attached by the customer to a service request (e.g. a picture of the
/// leaking pipe). Valuable visual context for experts preparing proposals.
/// </summary>
public class RequestImage : BaseEntity
{
    public int RequestId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation
    public ServiceRequest Request { get; set; } = null!;
}
