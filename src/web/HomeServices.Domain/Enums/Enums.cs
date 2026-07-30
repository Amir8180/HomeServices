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
    Disputed = 7
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
    Wallet = 3
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
    SiteBanner = 10
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
