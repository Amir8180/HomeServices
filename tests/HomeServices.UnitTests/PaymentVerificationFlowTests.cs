using HomeServices.Application.Dtos;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.UnitTests;

/// <summary>
/// شیت «امور مالی» داشبورد پشتیبانی — چرخهٔ کامل گزارش پرداخت کارت به کارت:
/// ثبت گزارش توسط مشتری → بررسی پشتیبانی (تأیید/رد) → ساخت رکورد Payment و
/// آزادسازی سفارش برای شروع کار کارشناس.
/// </summary>
public class PaymentVerificationFlowTests : IDisposable
{
    private readonly TestHost _host = new();

    [Fact]
    public async Task CustomerSubmitsReport_OrderMovesToWaitingVerification()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);

        var dto = new CreatePaymentVerificationReportDto
        {
            OrderId = orderId,
            Amount = 100_000m,
            SenderFullName = "علی احمدی",
            BankRefNumber = "12345",
            CustomerNote = "واریز انجام شد",
        };
        var report = await _host.PaymentReports.CreateAsync(dto, TestHost.CustomerA);

        Assert.Equal(PaymentVerificationStatus.PendingReview, report.Status);
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.WaitingPaymentVerification, order!.Status);
    }

    [Fact]
    public async Task NonOwnerCannotSubmitReport()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);

        var dto = new CreatePaymentVerificationReportDto { OrderId = orderId, Amount = 1m, SenderFullName = "سایر" };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _host.PaymentReports.CreateAsync(dto, TestHost.CustomerB));
    }

    [Fact]
    public async Task SupportVerifies_PaymentCreatedAndOrderPaid()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);
        var report = await _host.PaymentReports.CreateAsync(
            new CreatePaymentVerificationReportDto { OrderId = orderId, Amount = 100_000m, SenderFullName = "علی احمدی" },
            TestHost.CustomerA);

        var ok = await _host.PaymentReports.VerifyAsync(report.Id, "رسید بررسی شد", TestHost.AdminId);

        Assert.True(ok);
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.Paid, order!.Status);

        var payment = await _host.Uow.Repository<Payment>().GetAllNoTracking()
            .SingleAsync(p => p.OrderId == orderId);
        Assert.Equal(PaymentMethod.CardToCard, payment.PaymentMethod);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(100_000m, payment.Amount);

        var verified = await _host.PaymentReports.GetByIdAsync(report.Id);
        Assert.Equal(PaymentVerificationStatus.Verified, verified!.Status);
        Assert.Equal(TestHost.AdminId, verified.ReviewedBy);
        Assert.NotNull(verified.ReviewedAt);
    }

    [Fact]
    public async Task SupportRejects_OrderStaysAwaiting()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);
        var report = await _host.PaymentReports.CreateAsync(
            new CreatePaymentVerificationReportDto { OrderId = orderId, Amount = 100_000m, SenderFullName = "علی احمدی" },
            TestHost.CustomerA);

        var ok = await _host.PaymentReports.RejectAsync(report.Id, "رسید نامعتبر", TestHost.AdminId);

        Assert.True(ok);
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.WaitingPaymentVerification, order!.Status);
        Assert.Null(await _host.Uow.Repository<Payment>().GetAllNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId));
    }

    [Fact]
    public async Task VerifyingTwice_ReturnsFalse()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);
        var report = await _host.PaymentReports.CreateAsync(
            new CreatePaymentVerificationReportDto { OrderId = orderId, Amount = 100_000m, SenderFullName = "علی احمدی" },
            TestHost.CustomerA);

        Assert.True(await _host.PaymentReports.VerifyAsync(report.Id, null, TestHost.AdminId));
        Assert.False(await _host.PaymentReports.VerifyAsync(report.Id, null, TestHost.AdminId));
    }

    [Fact]
    public async Task FilterByStatus_ReturnsMatchingRecordsOnly()
    {
        // Order 1 → will be verified; Order 2 → stays pending.
        var (order1, _) = await _host.SeedPaidOrderAsync(status: OrderStatus.PendingPayment);
        var (order2, _) = await _host.SeedPaidOrderAsync(50_000m, OrderStatus.PendingPayment);

        var r1 = await _host.PaymentReports.CreateAsync(
            new CreatePaymentVerificationReportDto { OrderId = order1, Amount = 100_000m, SenderFullName = "الف" }, TestHost.CustomerA);
        await _host.PaymentReports.CreateAsync(
            new CreatePaymentVerificationReportDto { OrderId = order2, Amount = 50_000m, SenderFullName = "ب" }, TestHost.CustomerA);
        await _host.PaymentReports.VerifyAsync(r1.Id, null, TestHost.AdminId);

        var verified = await _host.PaymentReports.GetPagedAsync(new PaymentVerificationFilterDto { Status = PaymentVerificationStatus.Verified });
        var pending = await _host.PaymentReports.GetPagedAsync(new PaymentVerificationFilterDto { Status = PaymentVerificationStatus.PendingReview });

        Assert.Single(verified.Items);
        Assert.Equal(order1, verified.Items[0].OrderId);
        Assert.Single(pending.Items);
        Assert.Equal(order2, pending.Items[0].OrderId);
    }

    public void Dispose() => _host.Dispose();
}
