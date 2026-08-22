using System.Security.Cryptography;
using System.Text;

namespace HomeServices.Mvc.Extensions;

/// <summary>
/// کپچای ریاضی بدون وضعیت (Stateless):
/// دو عدد تصادفی تولید می‌شود و پاسخ درست داخل یک هش امضاشده برای کلاینت فرستاده می‌شود.
/// در پست‌بک، هش اعتبارسنجی می‌شود (تا اعداد دستکاری نشوند) و پاسخ کاربر مقایسه می‌گردد.
/// </summary>
public static class MathCaptcha
{
    // نمک ثابت امضا — تغییر آن کپچاهای در جریان را باطل می‌کند
    private const string Salt = "HS-Captcha-2026-v1";

    /// <summary>تولید مسئله جدید: (عدد اول، عدد دوم، هش پاسخ)</summary>
    public static (int A, int B, string Hash) Generate()
    {
        var a = Random.Shared.Next(2, 10);
        var b = Random.Shared.Next(2, 10);
        return (a, b, HashFor(a, b));
    }

    /// <summary>هش امضاشده‌ی پاسخ دو عدد.</summary>
    public static string HashFor(int a, int b)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{a}+{b}|{Salt}"));
        return Convert.ToHexString(bytes)[..16];
    }

    /// <summary>بررسی صحت پاسخ کاربر نسبت به مسئله‌ی ارسالی (ضد دستکاری).</summary>
    public static bool Verify(int? a, int? b, string? hash, string? answer)
    {
        if (a is null || b is null || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(answer))
            return false;
        if (!int.TryParse(answer.Trim(), out var ans))
            return false;
        return HashFor(a.Value, b.Value) == hash && ans == a.Value + b.Value;
    }
}
