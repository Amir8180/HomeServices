using HomeServices.Application.Dtos;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.UnitTests;

/// <summary>
/// شیت «نظارت بر فرآیندها» داشبورد پشتیبانی — اعلام اتمام کار دوسویه (کارشناس و مشتری)
/// همراه با توضیحات و مستندات، داوری پشتیبانی، تسویهٔ دستمزد ۹۰/۱۰ و بازگشت به
/// «در حال انجام» در صورت عدم تأیید.
/// </summary>
public class WorkCompletionFlowTests : IDisposable
{
    private readonly TestHost _host = new();

    [Fact]
    public async Task ExpertDeclares_ReportCreatedAndOrderMovesToCompletionReview()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync();

        var dto = new CreateWorkCompletionDeclarationDto
        {
            OrderId = orderId,
            Confirmed = true,
            Note = "کار با کیفیت انجام شد",
            FileUrls = new List<string> { "/uploads/a.jpg", "/uploads/b.mp4" },
            ThumbnailUrls = new List<string?> { "/uploads/a-thumb.jpg", null },
            MediaTypes = new List<MediaType> { MediaType.Image, MediaType.Video },
        };
        var report = await _host.Completions.DeclareCompletionAsync(dto, TestHost.ExpertX, AttachmentUploader.Expert);

        Assert.True(report.ExpertConfirmed);
        Assert.False(report.CustomerConfirmed);
        Assert.Equal("کار با کیفیت انجام شد", report.ExpertNote);
        Assert.Equal(2, report.Attachments.Count);
        Assert.Equal(MediaType.Video, report.Attachments[1].MediaType);
        Assert.Equal(AttachmentUploader.Expert, report.Attachments[0].Uploader);

        // Even a single-side declaration is immediately visible to support.
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.CompletionReview, order!.Status);
    }

    [Fact]
    public async Task CustomerDeclaresOnSameReport_BothSidesRecorded()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync();

        await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true, Note = "از سمت کارشناس" },
            TestHost.ExpertX, AttachmentUploader.Expert);

        var updated = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = false, Note = "راضی نیستم — قسمت پایین هنوز نشتی دارد" },
            TestHost.CustomerA, AttachmentUploader.Customer);

        Assert.True(updated.ExpertConfirmed);
        Assert.False(updated.CustomerConfirmed);
        Assert.Contains("نشتی", updated.CustomerNote);

        // Exactly ONE report per order.
        var count = await _host.Uow.Repository<WorkCompletionReport>().GetAllNoTracking()
            .CountAsync(r => r.OrderId == orderId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task StrangerCannotDeclare()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _host.Completions.DeclareCompletionAsync(
                new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true },
                TestHost.CustomerB, AttachmentUploader.Customer));
    }

    [Fact]
    public async Task SupportApproves_OrderCompletedAndPayout90_10()
    {
        var (orderId, orderNumber) = await _host.SeedPaidOrderAsync(amount: 200_000m);
        var report = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true },
            TestHost.ExpertX, AttachmentUploader.Expert);

        var ok = await _host.Completions.ApproveAsync(report.Id, "مستندات کافی بود", TestHost.AdminId);

        Assert.True(ok);
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.Completed, order!.Status);
        Assert.NotNull(order.CompletedDate);

        var request = await _host.Uow.Repository<ServiceRequest>().GetByIdAsync(order.RequestId);
        Assert.Equal(RequestStatus.Completed, request!.Status);

        var payout = await _host.Uow.Repository<ExpertPayout>().GetAllNoTracking()
            .SingleAsync(p => p.OrderId == orderId);
        Assert.Equal(200_000m, payout.GrossAmount);
        Assert.Equal(10m, payout.CommissionPercent);
        Assert.Equal(20_000m, payout.CommissionAmount);
        Assert.Equal(180_000m, payout.NetAmount);
        Assert.Equal(orderNumber, payout.OrderNumber);
        Assert.Equal(TestHost.AdminId, payout.PaidBy);
        Assert.StartsWith("PO-", payout.PayoutNumber);

        var approved = await _host.Completions.GetByIdAsync(report.Id);
        Assert.Equal(CompletionReviewStatus.Approved, approved!.Status);
        Assert.Equal("مستندات کافی بود", approved.SupportNote);
    }

    [Fact]
    public async Task SupportApprovesTwice_ReturnsFalse_AndNoDuplicatePayout()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync();
        var report = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true },
            TestHost.ExpertX, AttachmentUploader.Expert);

        Assert.True(await _host.Completions.ApproveAsync(report.Id, null, TestHost.AdminId));
        Assert.False(await _host.Completions.ApproveAsync(report.Id, null, TestHost.AdminId));

        var payouts = await _host.Uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Where(p => p.OrderId == orderId).ToListAsync();
        Assert.Single(payouts);
    }

    [Fact]
    public async Task SupportRejects_OrderReturnsToInProgressWithNote()
    {
        var (orderId, _) = await _host.SeedPaidOrderAsync();
        var report = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true },
            TestHost.ExpertX, AttachmentUploader.Expert);

        var ok = await _host.Completions.RejectAsync(report.Id, "دیوار نیازی رنگ نشده — لطفاً تکمیل گردد", TestHost.AdminId);

        Assert.True(ok);
        var order = await _host.Uow.Repository<Order>().GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.InProgress, order!.Status);

        var rejected = await _host.Completions.GetByIdAsync(report.Id);
        Assert.Equal(CompletionReviewStatus.Rejected, rejected!.Status);
        Assert.Contains("رنگ نشده", rejected.SupportNote);
    }

    [Fact]
    public async Task CustomCommissionPercent_FromSiteSettings_IsHonoured()
    {
        // Admin changes the commission rate to 15% before approval.
        await _host.SiteSettings.UpsertAsync(new UpsertSiteSettingDto
        {
            Key = "Payment.CommissionRatePercent",
            Value = "15",
            Group = "Payment",
        });

        var (orderId, _) = await _host.SeedPaidOrderAsync(amount: 100_000m);
        var report = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = orderId, Confirmed = true },
            TestHost.ExpertX, AttachmentUploader.Expert);
        await _host.Completions.ApproveAsync(report.Id, null, TestHost.AdminId);

        var payout = await _host.Uow.Repository<ExpertPayout>().GetAllNoTracking()
            .SingleAsync(p => p.OrderId == orderId);
        Assert.Equal(15m, payout.CommissionPercent);
        Assert.Equal(15_000m, payout.CommissionAmount);
        Assert.Equal(85_000m, payout.NetAmount);
    }

    [Fact]
    public async Task FilterByCompletionStatus_ReturnsMatchingRecordsOnly()
    {
        var (order1, _) = await _host.SeedPaidOrderAsync();
        var (order2, _) = await _host.SeedPaidOrderAsync(50_000m);

        var r1 = await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = order1, Confirmed = true }, TestHost.ExpertX, AttachmentUploader.Expert);
        await _host.Completions.DeclareCompletionAsync(
            new CreateWorkCompletionDeclarationDto { OrderId = order2, Confirmed = true }, TestHost.ExpertX, AttachmentUploader.Expert);
        await _host.Completions.ApproveAsync(r1.Id, null, TestHost.AdminId);

        var approved = await _host.Completions.GetPagedAsync(new CompletionReviewFilterDto { Status = CompletionReviewStatus.Approved });
        var pending = await _host.Completions.GetPagedAsync(new CompletionReviewFilterDto { Status = CompletionReviewStatus.PendingReview });

        Assert.Single(approved.Items);
        Assert.Equal(order1, approved.Items[0].OrderId);
        Assert.Single(pending.Items);
        Assert.Equal(order2, pending.Items[0].OrderId);
    }

    public void Dispose() => _host.Dispose();
}
