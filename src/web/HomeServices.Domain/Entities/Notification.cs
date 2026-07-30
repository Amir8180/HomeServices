using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// An in-app notification delivered to a user (customer or expert) about events
/// such as new proposals, accepted/rejected proposals, order status changes,
/// payments and reviews.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>Recipient user id (Guid) from the Identity service.</summary>
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationType Type { get; set; } = NotificationType.System;

    /// <summary>Optional deep-link URL the notification navigates to.</summary>
    public string? Url { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
