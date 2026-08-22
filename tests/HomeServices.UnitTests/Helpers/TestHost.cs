using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Application.Mappings;
using HomeServices.Application.Services;
using HomeServices.Domain.Common;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Infrastructure;
using HomeServices.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeServices.UnitTests.Helpers;

/// <summary>
/// In-memory test host wiring the real Application services against a SQLite
/// in-memory database: AppDbContext + UnitOfWork + AutoMapper + a pass-through
/// cache. Each test gets an isolated, fully-created schema.
/// </summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }
    public IUnitOfWork Uow { get; }
    public IMapper Mapper { get; }

    public TestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();

        Uow = new UnitOfWork(Context, NullLoggerFactory.Instance);
        Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    }

    // -------------------- Service factories (real implementations) --------------------
    public ISiteSettingService SiteSettings => new SiteSettingService(Uow, Mapper, new PassThroughCache(), NullLogger<SiteSettingService>.Instance);
    public IPaymentVerificationService PaymentReports => new PaymentVerificationService(Uow, Mapper, NullLogger<PaymentVerificationService>.Instance);
    public IWorkCompletionService Completions => new WorkCompletionService(Uow, Mapper, SiteSettings, NullLogger<WorkCompletionService>.Instance);
    public IExpertPayoutService Payouts => new ExpertPayoutService(Uow, Mapper, NullLogger<ExpertPayoutService>.Instance);
    public IProposalService Proposals => new ProposalService(Uow, Mapper, NullLogger<ProposalService>.Instance);
    public IOrderService Orders => new OrderService(Uow, Mapper, NullLogger<OrderService>.Instance);
    public IServiceRequestService Requests => new ServiceRequestService(Uow, Mapper, NullLogger<ServiceRequestService>.Instance);

    // -------------------- Seed helpers --------------------
    public static readonly Guid CustomerA = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerB = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ExpertX  = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFF0001");
    public static readonly Guid ExpertY  = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFF0002");
    public static readonly Guid AdminId  = new("99999999-9999-9999-9999-999999999999");

    private int _seq;

    /// <summary>Seeds category + request + a proposal from ExpertX and returns the ids.</summary>
    public async Task<(int RequestId, int ProposalId)> SeedOpenRequestWithProposalAsync(decimal price = 100_000m)
    {
        var n = ++_seq;
        var category = new Category { Name = "لوله‌کشی", Slug = $"plumbing-{n}", Group = CategoryGroup.Interior };
        await Uow.Repository<Category>().AddAsync(category);

        var request = new ServiceRequest
        {
            CustomerId = CustomerA,
            Category = category,
            Title = "نشتی لوله",
            Description = "تست",
            City = "تهران",
            Status = RequestStatus.Open,
        };
        await Uow.Repository<ServiceRequest>().AddAsync(request);

        var proposal = new Proposal
        {
            Request = request,
            ExpertId = (await EnsureExpertProfileAsync(ExpertX)).UserId,
            Price = price,
            Message = "انجام می‌دهم",
            Status = ProposalStatus.Pending,
        };
        await Uow.Repository<Proposal>().AddAsync(proposal);
        await Uow.SaveChangesAsync();
        Context.ChangeTracker.Clear(); // mimic a fresh per-request context after seeding

        return (request.Id, proposal.Id);
    }

    /// <summary>
    /// The EF model enforces Proposals.ExpertId → ExpertProfiles.UserId, mirroring the
    /// real app where an expert profile is provisioned on registration.
    /// </summary>
    public async Task<ExpertProfile> EnsureExpertProfileAsync(Guid userId)
    {
        var existing = await Uow.Repository<ExpertProfile>().GetAllNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing != null) return existing!;

        var profile = new ExpertProfile { UserId = userId, BusinessName = "تأسیسات تست" };
        await Uow.Repository<ExpertProfile>().AddAsync(profile);
        await Uow.SaveChangesAsync();
        return profile;
    }

    /// <summary>Seeds an order (request → accepted proposal → order) at the given status.</summary>
    public async Task<(int OrderId, string OrderNumber)> SeedPaidOrderAsync(decimal amount = 100_000m, OrderStatus status = OrderStatus.Paid)
    {
        var (requestId, proposalId) = await SeedOpenRequestWithProposalAsync(amount);
        var request = await Uow.Repository<ServiceRequest>().GetByIdAsync(requestId);
        var proposal = await Uow.Repository<Proposal>().GetByIdAsync(proposalId);

        proposal!.Status = ProposalStatus.Accepted;
        request!.Status = RequestStatus.Booked;
        request.AcceptedProposalId = proposalId;
        Uow.Repository<Proposal>().Update(proposal);
        Uow.Repository<ServiceRequest>().Update(request);

        var order = new Order
        {
            RequestId = requestId,
            ProposalId = proposalId,
            CustomerId = CustomerA,
            ExpertId = ExpertX,
            OrderNumber = $"HS-{100000 + _seq}",
            Status = status,
            TotalAmount = amount,
        };
        await Uow.Repository<Order>().AddAsync(order);
        await Uow.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return (order.Id, order.OrderNumber);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Cache double with no storage — every read goes to the factory (the DB).</summary>
public sealed class PassThroughCache : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) => await factory();
    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
