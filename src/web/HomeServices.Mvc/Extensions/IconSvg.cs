using Microsoft.AspNetCore.Html;

namespace HomeServices.Mvc.Extensions;

/// <summary>
/// کتابخانه آیکون‌های SVG (سبک Feather) قابل استفاده در تمام ویوها —
/// جایگزین ایموجی‌های ویندوز. مثال: <span>@IconSvg.Money @p.Price</span>
/// </summary>
public static class IconSvg
{
    private const string Wrap =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" " +
        "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\" " +
        "style=\"width:1em;height:1em;display:inline-block;vertical-align:-0.15em;flex-shrink:0;\">{0}</svg>";

    private static HtmlString Ico(string paths) => new(string.Format(Wrap, paths));

    /// <summary>اسکناس — قیمت/مبلغ</summary>
    public static HtmlString Money    => Ico("<rect x='2' y='6' width='20' height='12' rx='2'/><circle cx='12' cy='12' r='3'/>");
    /// <summary>ساعت — مدت/زمان</summary>
    public static HtmlString Clock    => Ico("<circle cx='12' cy='12' r='10'/><polyline points='12 6 12 12 16 14'/>");
    /// <summary>تقویم — تاریخ</summary>
    public static HtmlString Calendar => Ico("<rect x='3' y='4' width='18' height='18' rx='2'/><line x1='16' y1='2' x2='16' y2='6'/><line x1='8' y1='2' x2='8' y2='6'/><line x1='3' y1='10' x2='21' y2='10'/>");
    /// <summary>برچسب — دسته‌بندی</summary>
    public static HtmlString Tag      => Ico("<path d='M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.83z'/><line x1='7' y1='7' x2='7.01' y2='7'/>");
    /// <summary>نشان موقعیت — شهر/آدرس</summary>
    public static HtmlString Pin      => Ico("<path d='M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z'/><circle cx='12' cy='10' r='3'/>");
    /// <summary>تیک دایره‌ای — تأیید/موفق</summary>
    public static HtmlString Check    => Ico("<path d='M22 11.08V12a10 10 0 1 1-5.93-9.14'/><polyline points='22 4 12 14.01 9 11.01'/>");
    /// <summary>ذره‌بین — جستجو</summary>
    public static HtmlString Search   => Ico("<circle cx='11' cy='11' r='8'/><path d='M21 21l-4.35-4.35'/>");
    /// <summary>گفتگو — پیام/نظر</summary>
    public static HtmlString Message  => Ico("<path d='M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z'/>");
    /// <summary>برق — سرعت/پاسخ سریع</summary>
    public static HtmlString Zap      => Ico("<polygon points='13 2 3 14 12 14 11 22 21 10 12 10 13 2'/>");
    /// <summary>چکش — تعمیر/خدمات</summary>
    public static HtmlString Hammer   => Ico("<path d='M15 12l-8.5 8.5a2.12 2.12 0 0 1-3-3L12 9'/><path d='M17.64 15L22 10.64'/><path d='M20.91 11.7L16.3 7.09a2 2 0 0 0-2.83 0L11 9.5l4.5 4.5 5.41-2.3z'/>");
    /// <summary>ستاره — امتیاز</summary>
    public static HtmlString Star     => Ico("<polygon points='12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2'/>");
    /// <summary>کاربر — شخص</summary>
    public static HtmlString User     => Ico("<path d='M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2'/><circle cx='12' cy='7' r='4'/>");
    /// <summary>کیف کار — سابقه/پروژه</summary>
    public static HtmlString Briefcase => Ico("<rect x='2' y='7' width='20' height='14' rx='2'/><path d='M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16'/>");
    /// <summary>کیف پول — مدیریت مالی/درآمد</summary>
    public static HtmlString Wallet  => Ico("<path d='M20 7H4a2 2 0 0 1 0-4h14v4'/><path d='M20 7a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5'/><circle cx='17' cy='14' r='1'/>");
    /// <summary>لامپ — نکته/راهنما</summary>
    public static HtmlString Lightbulb => Ico("<path d='M9 18h6'/><path d='M10 22h4'/><path d='M12 2a7 7 0 0 0-4 12.7c.6.5 1 1.4 1 2.3h6c0-.9.4-1.8 1-2.3A7 7 0 0 0 12 2z'/>");
    /// <summary>روی نمودار — رشد/گزارش</summary>
    public static HtmlString TrendingUp => Ico("<polyline points='23 6 13.5 15.5 8.5 10.5 1 18'/><polyline points='17 6 23 6 23 12'/>");
    /// <summary>فاکتور — صورتحساب</summary>
    public static HtmlString FileText => Ico("<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><polyline points='14 2 14 8 20 8'/><line x1='16' y1='13' x2='8' y2='13'/><line x1='16' y1='17' x2='8' y2='17'/>");
}
