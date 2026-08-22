namespace HomeServices.Application.Dtos;

/// <summary>
/// Real platform statistics computed from the database.
/// Used by public pages (homepage stats banner, dashboards) so that
/// displayed numbers always reflect actual data instead of hard-coded values.
/// </summary>
public class PlatformStatsDto
{
    /// <summary>تعداد کارشناسان فعالِ تأییدشده.</summary>
    public int VerifiedExperts { get; set; }

    /// <summary>تعداد سفارش‌هایی که با موفقیت تکمیل شده‌اند.</summary>
    public int CompletedProjects { get; set; }

    /// <summary>تعداد کل نظرات منتشرشده (تأییدشده).</summary>
    public int TotalReviews { get; set; }

    /// <summary>میانگین امتیاز نظرات تأییدشده (۰ تا ۵) — بدون نظر، صفر است.</summary>
    public double AverageRating { get; set; }

    /// <summary>درصد نظرات ۴ و ۵ ستاره از میان نظرات تأییدشده — بدون نظر، صفر است.</summary>
    public int SatisfactionPercent { get; set; }

    /// <summary>آیا داده‌ای برای امتیاز/رضایت وجود دارد؟</summary>
    public bool HasRatingData => TotalReviews > 0;

    /// <summary>مجموع درآمد سایت (کمیسیون ۱۰٪ از سفارش‌های تکمیل‌شده).</summary>
    public decimal TotalSiteRevenue { get; set; }
}
