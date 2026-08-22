namespace HomeServices.Application.Common;

/// <summary>
/// Single source of truth for the manual card-to-card payment flow used across the
/// whole application. The platform does NOT use a payment gateway: the customer is
/// shown the card details on the order payment page and is redirected to the support
/// Telegram chat to send the transfer receipt.
///
/// Values can be overridden at runtime from the SiteSettings table (keys below);
/// the constants here act as safe defaults so the flow always renders.
/// </summary>
public static class CardToCardPaymentInfo
{
    // -------------------- SiteSettings keys --------------------
    public const string CardNumberSettingKey  = "Payment.CardNumber";
    public const string CardHolderSettingKey  = "Payment.CardHolder";
    public const string TelegramSettingKey    = "Payment.Telegram";
    public const string CommissionRateSettingKey = "Payment.CommissionRatePercent";

    // -------------------- Defaults ------
    public const string DefaultCardNumber = "6219861929428836";
    public const string DefaultCardHolder = "امیرحسین اکبرزاده نیاکی";
    public const string DefaultTelegramUsername = "Another81";

    /// <summary>Site commission percent kept from every completed order (10%).</summary>
    public const decimal DefaultCommissionPercent = 10m;

    /// <summary>
    /// Resolves the effective commission percent from the settings dictionary,
    /// falling back to the default 10% when the key is missing or invalid.
    /// Parsing is culture-invariant so "12.5" works on any server locale.
    /// </summary>
    public static decimal ResolveCommissionPercent(IReadOnlyDictionary<string, string?>? settings)
    {
        if (settings != null &&
            settings.TryGetValue(CommissionRateSettingKey, out var raw) &&
            decimal.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var percent) &&
            percent is >= 0 and <= 100)
        {
            return percent;
        }
        return DefaultCommissionPercent;
    }

    /// <summary>Full deep link to the support Telegram private chat (pv).</summary>
    public const string TelegramUrl = "https://t.me/" + DefaultTelegramUsername;

    /// <summary>
    /// The instruction shown to the customer on the payment page: after paying,
    /// send the transfer receipt in the same (Telegram) page.
    /// </summary>
    public const string ReceiptInstruction =
        "پس از پرداخت، لطفاً رسید تراکنش را در همین صفحه ارسال نمایید.";

    /// <summary>
    /// Resolves the effective card-to-card details, preferring SiteSettings values
    /// (loaded as a dictionary via ISiteSettingService.GetAllAsDictionaryAsync)
    /// and falling back to the hard-coded defaults when a key is missing.
    /// </summary>
    public static (string CardNumber, string CardHolder, string TelegramUrl) Resolve(
        IReadOnlyDictionary<string, string?>? settings)
    {
        var card = settings != null && settings.TryGetValue(CardNumberSettingKey, out var cn) && !string.IsNullOrWhiteSpace(cn)
            ? cn
            : DefaultCardNumber;

        var holder = settings != null && settings.TryGetValue(CardHolderSettingKey, out var ch) && !string.IsNullOrWhiteSpace(ch)
            ? ch
            : DefaultCardHolder;

        var telegram = settings != null && settings.TryGetValue(TelegramSettingKey, out var tg) && !string.IsNullOrWhiteSpace(tg)
            ? BuildTelegramUrl(tg)
            : TelegramUrl;

        return (card, holder, telegram);
    }

    /// <summary>Normalises a Telegram handle (@user, https://t.me/user, user) into a full chat URL.</summary>
    public static string BuildTelegramUrl(string handle)
    {
        var h = handle.Trim();
        if (h.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase) ||
            h.StartsWith("http://t.me/", StringComparison.OrdinalIgnoreCase))
            return h;
        return "https://t.me/" + h.TrimStart('@');
    }
}
