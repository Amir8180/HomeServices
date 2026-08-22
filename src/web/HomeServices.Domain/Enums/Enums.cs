namespace HomeServices.Domain.Enums;

/// <summary>
/// Top-level grouping of service categories, inspired by the way Angi groups
/// services on its homepage (Interior / Exterior / Lawn &amp; Garden / Other).
/// </summary>
public enum CategoryGroup
{
    Interior = 1,
    Exterior = 2,
    LawnGarden = 3,
    Other = 4
}

/// <summary>
/// Lifecycle of a customer's service request.
/// </summary>
public enum RequestStatus
{
    Draft = 1,
    Open = 2,
    Quoted = 3,
    Booked = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
    Expired = 8
}

/// <summary>
/// Lifecycle of an expert's proposal/quote on a request.
/// </summary>
public enum ProposalStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4
}

/// <summary>
/// Lifecycle of an order created once a customer accepts a proposal.
/// </summary>
public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Scheduled = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    Disputed = 7,

    /// <summary>Customer submitted the card-to-card receipt; waiting for support verification.</summary>
    WaitingPaymentVerification = 8,

    /// <summary>One or both sides declared work completion; waiting for support review/mediation.</summary>
    CompletionReview = 9
}

/// <summary>
/// Moderation state of a card-to-card payment report submitted by the customer
/// and reviewed by the site support team.
/// </summary>
public enum PaymentVerificationStatus
{
    /// <summary>بررسی نشده — submitted, awaiting support review.</summary>
    PendingReview = 1,

    /// <summary>پرداخت شده — support confirmed the transfer.</summary>
    Verified = 2,

    /// <summary>عدم پرداخت — support could not verify the receipt.</summary>
    Rejected = 3
}

/// <summary>
/// Moderation state of a dual work-completion declaration (expert + customer)
/// reviewed by the site support team as mediator.
/// </summary>
public enum CompletionReviewStatus
{
    /// <summary>بررسی نشده — awaiting support review (sent as soon as either side declares).</summary>
    PendingReview = 1,

    /// <summary>تأیید شده — support approved completion; payout is released.</summary>
    Approved = 2,

    /// <summary>عدم تأیید — support rejected; returned to both parties for re-work/re-review.</summary>
    Rejected = 3
}

/// <summary>Which side uploaded a work-completion attachment.</summary>
public enum AttachmentUploader
{
    Expert = 1,
    Customer = 2
}

/// <summary>
/// State of a payment for an order.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}

/// <summary>
/// Method used to pay for an order.
/// </summary>
public enum PaymentMethod
{
    Online = 1,
    Cash = 2,
    Wallet = 3,

    /// <summary>Manual card-to-card bank transfer, verified via the support Telegram chat.</summary>
    CardToCard = 4
}

/// <summary>
/// How urgently the customer needs the service (Angi-style qualifying question).
/// </summary>
public enum UrgencyLevel
{
    Emergency = 1,
    Within24Hours = 2,
    WithinAWeek = 3,
    Flexible = 4
}

/// <summary>
/// Moderation state of a customer review.
/// </summary>
public enum ReviewStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

/// <summary>
/// The kind of media asset stored in the central media table.
/// </summary>
public enum MediaType
{
    Image = 1,
    Video = 2,
    Document = 3
}

/// <summary>
/// The entity type a media asset belongs to (for the generic media library).
/// </summary>
public enum MediaEntityType
{
    Service = 1,
    ServiceImage = 2,
    Request = 3,
    ExpertLogo = 4,
    ExpertCover = 5,
    ExpertPortfolio = 6,
    UserAvatar = 7,
    SiteLogo = 8,
    SiteFavicon = 9,
    SiteBanner = 10,
    PaymentReceipt = 11,
    CompletionAttachment = 12,

    /// <summary>پیوست تیکت پشتیبانی.</summary>
    SupportTicketAttachment = 13
}

/// <summary>
/// Type of an in-app notification.
/// </summary>
public enum NotificationType
{
    NewProposal = 1,
    ProposalAccepted = 2,
    ProposalRejected = 3,
    OrderStatusChanged = 4,
    PaymentReceived = 5,
    NewReview = 6,
    System = 7
}

/// <summary>Lifecycle of a user-submitted support ticket.</summary>
public enum SupportTicketStatus
{
    /// <summary>باز — ثبت‌شده توسط کاربر، در انتظار بررسی پشتیبانی.</summary>
    Open = 1,

    /// <summary>در حال بررسی — پشتیبانی در حال پیگیری است.</summary>
    InProgress = 2,

    /// <summary>حل شده — پاسخ نهایی توسط پشتیبانی ارائه شده است.</summary>
    Resolved = 3,

    /// <summary>بسته — توسط کاربر یا پشتیبانی بسته شده است.</summary>
    Closed = 4
}

/// <summary>Subject categories of a support ticket.</summary>
public enum SupportTicketCategory
{
    /// <summary>مشکل در سفارش</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "مشکل در سفارش")]
    OrderIssue = 1,

    /// <summary>امور مالی و پرداخت</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "امور مالی و پرداخت")]
    Payment = 2,

    /// <summary>مشکل فنی سایت</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "مشکل فنی سایت")]
    Technical = 3,

    /// <summary>حساب کاربری</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "حساب کاربری")]
    Account = 4,

    /// <summary>پیشنهاد و انتقاد</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "پیشنهاد و انتقاد")]
    Suggestion = 5,

    /// <summary>سایر</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "سایر")]
    Other = 6
}

/// <summary>Urgency of a support ticket.</summary>
public enum SupportTicketPriority
{
    [System.ComponentModel.DataAnnotations.Display(Name = "کم")]
    Low = 1,

    [System.ComponentModel.DataAnnotations.Display(Name = "معمولی")]
    Normal = 2,

    [System.ComponentModel.DataAnnotations.Display(Name = "زیاد")]
    High = 3,

    [System.ComponentModel.DataAnnotations.Display(Name = "فوری")]
    Urgent = 4
}
