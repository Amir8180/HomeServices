using HomeServices.Domain.Enums;

namespace HomeServices.Mvc.Extensions;

/// <summary>
/// Persian display strings for domain enums and small UI helpers used across views.
/// Keeps enum-to-label mapping in one place so views stay clean.
/// </summary>
public static class DisplayExtensions
{
    public static string ToDisplay(this CategoryGroup g) => g switch
    {
        CategoryGroup.Interior => "داخلی",
        CategoryGroup.Exterior => "بیرونی",
        CategoryGroup.LawnGarden => "باغ و باغچه",
        CategoryGroup.Other => "سایر",
        _ => g.ToString(),
    };

    public static string ToDisplay(this RequestStatus s) => s switch
    {
        RequestStatus.Draft => "پیش‌نویس",
        RequestStatus.Open => "باز",
        RequestStatus.Quoted => "دارای پیشنهاد",
        RequestStatus.Booked => "رزرو شده",
        RequestStatus.InProgress => "در حال انجام",
        RequestStatus.Completed => "تکمیل شده",
        RequestStatus.Cancelled => "لغو شده",
        RequestStatus.Expired => "منقضی",
        _ => s.ToString(),
    };

    public static string ToDisplay(this ProposalStatus s) => s switch
    {
        ProposalStatus.Pending => "در انتظار",
        ProposalStatus.Accepted => "پذیرفته شده",
        ProposalStatus.Rejected => "رد شده",
        ProposalStatus.Withdrawn => "برگشت‌خورده",
        _ => s.ToString(),
    };

    public static string ToDisplay(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "در انتظار پرداخت",
        OrderStatus.Paid => "پرداخت شده",
        OrderStatus.Scheduled => "زمان‌بندی شده",
        OrderStatus.InProgress => "در حال انجام",
        OrderStatus.Completed => "تکمیل شده",
        OrderStatus.Cancelled => "لغو شده",
        OrderStatus.Disputed => "مورد اختلاف",
        _ => s.ToString(),
    };

    public static string ToDisplay(this PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "در انتظار",
        PaymentStatus.Succeeded => "موفق",
        PaymentStatus.Failed => "ناموفق",
        PaymentStatus.Refunded => "بازگشت‌خورده",
        PaymentStatus.PartiallyRefunded => "بازگشت جزئی",
        _ => s.ToString(),
    };

    public static string ToDisplay(this PaymentMethod m) => m switch
    {
        PaymentMethod.Online => "آنلاین",
        PaymentMethod.Cash => "نقدی",
        PaymentMethod.Wallet => "کیف پول",
        _ => m.ToString(),
    };

    public static string ToDisplay(this UrgencyLevel u) => u switch
    {
        UrgencyLevel.Emergency => "اضطراری",
        UrgencyLevel.Within24Hours => "ظرف ۲۴ ساعت",
        UrgencyLevel.WithinAWeek => "ظرف یک هفته",
        UrgencyLevel.Flexible => "انعطاف‌پذیر",
        _ => u.ToString(),
    };

    public static string ToDisplay(this ReviewStatus s) => s switch
    {
        ReviewStatus.Pending => "در انتظار بازبینی",
        ReviewStatus.Approved => "تأیید شده",
        ReviewStatus.Rejected => "رد شده",
        _ => s.ToString(),
    };

    /// <summary>
    /// Returns a Persian (Jalali) short date string. Falls back to Gregorian when
    /// the System.Globalization.PersianCalendar is unavailable. Avoids pulling in an
    /// external Persian calendar NuGet dependency for this resume project.
    /// </summary>
    public static string ToPersianDate(this DateTime dt)
    {
        try
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(dt):0000}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00}";
        }
        catch
        {
            return dt.ToString("yyyy/MM/dd");
        }
    }

    public static string ToPersianDateTime(this DateTime dt)
        => $"{dt.ToPersianDate()} - {dt:HH:mm}";

    public static string StatusClass(this RequestStatus s) => s switch
    {
        RequestStatus.Open => "badge-info-soft",
        RequestStatus.Quoted => "badge-info-soft",
        RequestStatus.Booked => "badge-brand",
        RequestStatus.InProgress => "badge-warning-soft",
        RequestStatus.Completed => "badge-success-soft",
        RequestStatus.Cancelled or RequestStatus.Expired => "badge-warning-soft",
        _ => "badge-info-soft",
    };

    public static string StatusClass(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "badge-warning-soft",
        OrderStatus.Paid => "badge-info-soft",
        OrderStatus.Scheduled => "badge-brand",
        OrderStatus.InProgress => "badge-warning-soft",
        OrderStatus.Completed => "badge-success-soft",
        OrderStatus.Cancelled => "badge-warning-soft",
        OrderStatus.Disputed => "badge-warning-soft",
        _ => "badge-info-soft",
    };

    public static string ProposalStatusClass(this ProposalStatus s) => s switch
    {
        ProposalStatus.Pending => "badge-info-soft",
        ProposalStatus.Accepted => "badge-success-soft",
        ProposalStatus.Rejected => "badge-warning-soft",
        ProposalStatus.Withdrawn => "badge-warning-soft",
        _ => "badge-info-soft",
    };

    public static string StatusClass(this ProposalStatus s) => s.ProposalStatusClass();

    public static string ReviewStatusClass(this ReviewStatus s) => s switch
    {
        ReviewStatus.Pending => "badge-warning-soft",
        ReviewStatus.Approved => "badge-success-soft",
        ReviewStatus.Rejected => "badge-warning-soft",
        _ => "badge-info-soft",
    };

    public static string StatusClass(this ReviewStatus s) => s.ReviewStatusClass();
}
