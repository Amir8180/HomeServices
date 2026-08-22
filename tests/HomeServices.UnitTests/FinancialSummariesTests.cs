using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.UnitTests;

/// <summary>
/// صفحه «مدیریت مالی» کارشناس و صفحه «درآمد سایت» ادمین — جمع درآمد روزانه/هفتگی/
/// ماهانه/سالانه از روی تسویه‌های واقعی دیتابیس و تفکیک کمیسیون سایت.
/// </summary>
public class FinancialSummariesTests : IDisposable
{
    private readonly TestHost _host = new();

    private async Task<int> AddPayoutAsync(Guid expertId, decimal netAmount, decimal commission, DateTime paidAt)
    {
        var payout = new ExpertPayout
        {
            PayoutNumber = $"PO-{100000 + ++_seq}",
            OrderId = 0,
            ExpertId = expertId,
            CustomerId = TestHost.CustomerA,
            GrossAmount = netAmount + commission,
            CommissionPercent = 10m,
            CommissionAmount = commission,
            NetAmount = netAmount,
            OrderNumber = $"HS-{200000 + _seq}",
            ServiceTitle = "تست خدمت",
            PaidAt = paidAt,
        };

        // Create a backing order to satisfy the FK (payout has 1:1 with Order).
        var (orderId, orderNumber) = await _host.SeedPaidOrderAsync(payout.GrossAmount, OrderStatus.Completed);
        payout.OrderId = orderId;
        payout.OrderNumber = orderNumber;

        await _host.Uow.Repository<ExpertPayout>().AddAsync(payout);
        await _host.Uow.SaveChangesAsync();
        return payout.Id;
    }

    private int _seq;

    [Fact]
    public async Task ExpertIncomeSummary_SumsNetAmountsPerPeriod()
    {
        var now = DateTime.UtcNow;
        await AddPayoutAsync(TestHost.ExpertX, 90_000m, 10_000m, now);                    // today
        await AddPayoutAsync(TestHost.ExpertX, 45_000m, 5_000m, now.AddDays(-3));         // this week
        await AddPayoutAsync(TestHost.ExpertX, 30_000m, 3_333m, now.AddMonths(-2));       // this year
        await AddPayoutAsync(TestHost.ExpertY, 999_999m, 1m, now);                        // another expert

        var summary = await _host.Payouts.GetExpertIncomeSummaryAsync(TestHost.ExpertX, "daily");

        Assert.Equal(165_000m, summary.TotalIncome);
        Assert.Equal(3, summary.TotalPayouts);
        Assert.Equal(90_000m, summary.TodayIncome);
        Assert.Equal(135_000m, summary.ThisWeekIncome);
        Assert.Equal(135_000m, summary.ThisMonthIncome);
        Assert.Equal(165_000m, summary.ThisYearIncome);
    }

    [Fact]
    public async Task SiteRevenueSummary_SumsCommissions()
    {
        var now = DateTime.UtcNow;
        await AddPayoutAsync(TestHost.ExpertX, 90_000m, 10_000m, now);
        await AddPayoutAsync(TestHost.ExpertY, 45_000m, 5_000m, now);

        var summary = await _host.Payouts.GetSiteRevenueSummaryAsync("daily");

        Assert.Equal(15_000m, summary.TotalRevenue);
        Assert.Equal(2, summary.TotalPayouts);
        Assert.Equal(15_000m, summary.TodayRevenue);
    }

    [Fact]
    public async Task PayoutsList_FilteredByExpert_OnlyOwnRecords()
    {
        var now = DateTime.UtcNow;
        await AddPayoutAsync(TestHost.ExpertX, 90_000m, 10_000m, now);
        await AddPayoutAsync(TestHost.ExpertY, 45_000m, 5_000m, now);

        var mine = await _host.Payouts.GetPagedAsync(new Application.Dtos.ExpertPayoutFilterDto { ExpertId = TestHost.ExpertX });

        Assert.Single(mine.Items);
        Assert.Equal(TestHost.ExpertX, mine.Items[0].ExpertId);
        Assert.Equal(90_000m, mine.Items[0].NetAmount);
        Assert.Equal("تست خدمت", mine.Items[0].ServiceTitle);
    }

    [Fact]
    public async Task CommissionResolution_FallsBackTo10Percent()
    {
        // No site setting rows at all → default 10%.
        Assert.Equal(10m, Application.Common.CardToCardPaymentInfo.ResolveCommissionPercent(null));
        Assert.Equal(10m, Application.Common.CardToCardPaymentInfo.ResolveCommissionPercent(
            new Dictionary<string, string?> { ["Payment.CommissionRatePercent"] = "garbage" }));
        Assert.Equal(12.5m, Application.Common.CardToCardPaymentInfo.ResolveCommissionPercent(
            new Dictionary<string, string?> { ["Payment.CommissionRatePercent"] = "12.5" }));
    }

    public void Dispose() => _host.Dispose();
}
