using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.UnitTests;

/// <summary>
/// نقطه شروع چرخهٔ کامل: انتخاب پیشنهاد توسط مشتری → ساخت سفارش در وضعیت
/// «در انتظار پرداخت» → مسیر پرداخت کارت به کارت.
/// </summary>
public class ProposalAcceptanceFlowTests : IDisposable
{
    private readonly TestHost _host = new();

    [Fact]
    public async Task AcceptProposal_CreatesOrderInPendingPayment()
    {
        var (requestId, proposalId) = await _host.SeedOpenRequestWithProposalAsync(price: 250_000m);

        var accepted = await _host.Proposals.AcceptAsync(proposalId, TestHost.CustomerA);
        Assert.True(accepted);

        var order = await _host.Orders.CreateFromProposalAsync(proposalId, TestHost.CustomerA);

        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(250_000m, order.TotalAmount);
        Assert.Equal(TestHost.CustomerA, order.CustomerId);
        Assert.Equal(TestHost.ExpertX, order.ExpertId);
        Assert.Matches("^HS-\\d+$", order.OrderNumber);

        var request = await _host.Uow.Repository<ServiceRequest>().GetByIdAsync(requestId);
        Assert.Equal(RequestStatus.Booked, request!.Status);
        Assert.Equal(proposalId, request.AcceptedProposalId);
    }

    [Fact]
    public async Task AcceptProposal_RejectsSiblingProposals()
    {
        var (requestId, proposalId) = await _host.SeedOpenRequestWithProposalAsync();

        var rival = new Proposal
        {
            RequestId = requestId,
            ExpertId = (await _host.EnsureExpertProfileAsync(TestHost.ExpertY)).UserId,
            Price = 300_000m,
            Status = ProposalStatus.Pending,
        };
        await _host.Uow.Repository<Proposal>().AddAsync(rival);
        await _host.Uow.SaveChangesAsync();
        _host.Context.ChangeTracker.Clear();

        await _host.Proposals.AcceptAsync(proposalId, TestHost.CustomerA);

        var rivalAfter = await _host.Uow.Repository<Proposal>().GetByIdAsync(rival.Id);
        Assert.Equal(ProposalStatus.Rejected, rivalAfter!.Status);

        var chosen = await _host.Uow.Repository<Proposal>().GetByIdAsync(proposalId);
        Assert.Equal(ProposalStatus.Accepted, chosen!.Status);
    }

    [Fact]
    public async Task NonOwnerCustomerCannotAccept()
    {
        var (_, proposalId) = await _host.SeedOpenRequestWithProposalAsync();

        Assert.False(await _host.Proposals.AcceptAsync(proposalId, TestHost.CustomerB));
    }

    [Fact]
    public async Task OrderFromSameProposal_IsNotDuplicated()
    {
        var (_, proposalId) = await _host.SeedOpenRequestWithProposalAsync();
        await _host.Proposals.AcceptAsync(proposalId, TestHost.CustomerA);

        var first = await _host.Orders.CreateFromProposalAsync(proposalId, TestHost.CustomerA);
        var second = await _host.Orders.CreateFromProposalAsync(proposalId, TestHost.CustomerA);

        Assert.Equal(first.Id, second.Id);
    }

    public void Dispose() => _host.Dispose();
}
