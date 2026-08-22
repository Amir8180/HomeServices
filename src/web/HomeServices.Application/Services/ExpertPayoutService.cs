using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

public class ExpertPayoutService : IExpertPayoutService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ExpertPayoutService> _logger;

    public ExpertPayoutService(IUnitOfWork uow, IMapper mapper, ILogger<ExpertPayoutService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<ExpertPayoutDto>> GetPagedAsync(ExpertPayoutFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Include(p => p.Order).AsQueryable();

        if (filter.ExpertId.HasValue) query = query.Where(p => p.ExpertId == filter.ExpertId.Value);
        if (filter.FromDate.HasValue) query = query.Where(p => p.PaidAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(p => p.PaidAt <= filter.ToDate.Value);

        query = query.OrderByDescending(p => p.PaidAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ExpertPayoutDto>
        {
            Items = _mapper.Map<List<ExpertPayoutDto>>(items),
            TotalCount = total, PageNumber = page, PageSize = pageSize,
        };
    }

    public async Task<ExpertPayoutDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<ExpertPayoutDto>(entity);
    }

    public async Task<ExpertIncomeSummaryDto> GetExpertIncomeSummaryAsync(Guid expertId, string period, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // Materialise (PaidAt, NetAmount) pairs first: decimal Sum is not translated by
        // every provider (e.g. SQLite), and per-expert payout sets are small.
        var rows = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Where(p => p.ExpertId == expertId && p.PaidAt != null)
            .Select(p => new { p.PaidAt, p.NetAmount })
            .ToListAsync(cancellationToken);

        var summary = new ExpertIncomeSummaryDto
        {
            TotalIncome = rows.Sum(r => r.NetAmount),
            TotalPayouts = rows.Count,
            TodayIncome = rows.Where(r => r.PaidAt >= now.Date).Sum(r => r.NetAmount),
            ThisWeekIncome = rows.Where(r => r.PaidAt >= now.Date.AddDays(-(int)now.DayOfWeek)).Sum(r => r.NetAmount),
            ThisMonthIncome = rows.Where(r => r.PaidAt >= new DateTime(now.Year, now.Month, 1)).Sum(r => r.NetAmount),
            ThisYearIncome = rows.Where(r => r.PaidAt >= new DateTime(now.Year, 1, 1)).Sum(r => r.NetAmount),
        };

        summary.ChartData = BuildChartData(rows.Select(r => (r.PaidAt!.Value, r.NetAmount)), period);
        return summary;
    }

    public async Task<SiteRevenueSummaryDto> GetSiteRevenueSummaryAsync(string period, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Where(p => p.PaidAt != null)
            .Select(p => new { p.PaidAt, p.CommissionAmount })
            .ToListAsync(cancellationToken);

        var summary = new SiteRevenueSummaryDto
        {
            TotalRevenue = rows.Sum(r => r.CommissionAmount),
            TotalPayouts = rows.Count,
            TodayRevenue = rows.Where(r => r.PaidAt >= now.Date).Sum(r => r.CommissionAmount),
            ThisWeekRevenue = rows.Where(r => r.PaidAt >= now.Date.AddDays(-(int)now.DayOfWeek)).Sum(r => r.CommissionAmount),
            ThisMonthRevenue = rows.Where(r => r.PaidAt >= new DateTime(now.Year, now.Month, 1)).Sum(r => r.CommissionAmount),
            ThisYearRevenue = rows.Where(r => r.PaidAt >= new DateTime(now.Year, 1, 1)).Sum(r => r.CommissionAmount),
        };

        summary.ChartData = BuildChartData(rows.Select(r => (r.PaidAt!.Value, r.CommissionAmount)), period);
        return summary;
    }

    public async Task<List<IncomeChartPointDto>> GetExpertMonthlyTrendAsync(Guid expertId, int months = 6, CancellationToken cancellationToken = default)
    {
        if (months < 1) months = 6;
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

        var rows = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Where(p => p.ExpertId == expertId && p.PaidAt >= start)
            .Select(p => new { p.PaidAt, p.NetAmount })
            .ToListAsync(cancellationToken);

        var byMonth = rows
            .GroupBy(r => new DateTime(r.PaidAt!.Value.Year, r.PaidAt!.Value.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount));

        var trend = new List<IncomeChartPointDto>();
        for (int i = 0; i < months; i++)
        {
            var month = start.AddMonths(i);
            trend.Add(new IncomeChartPointDto
            {
                Label = month.ToString("yyyy-MM"),
                Amount = byMonth.TryGetValue(month, out var amount) ? amount : 0,
            });
        }
        return trend;
    }

    private static List<IncomeChartPointDto> BuildChartData(IEnumerable<(DateTime PaidAt, decimal Amount)> rows, string period)    {
        var now = DateTime.UtcNow;

        switch (period?.ToLowerInvariant())
        {
            case "weekly":
                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                return rows.Where(r => r.PaidAt >= weekStart)
                    .GroupBy(r => r.PaidAt.Date)
                    .Select(g => new IncomeChartPointDto { Label = g.Key.ToString("MM/dd"), Amount = g.Sum(x => x.Amount) })
                    .OrderBy(p => p.Label)
                    .ToList();

            case "monthly":
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var allDays = Enumerable.Range(0, daysInMonth).Select(d => new IncomeChartPointDto
                {
                    Label = new DateTime(now.Year, now.Month, d + 1).ToString("MM/dd"),
                    Amount = 0
                }).ToList();

                foreach (var g in rows.Where(r => r.PaidAt >= monthStart).GroupBy(r => r.PaidAt.Date))
                {
                    var label = g.Key.ToString("MM/dd");
                    var existing = allDays.FirstOrDefault(a => a.Label == label);
                    if (existing != null) existing.Amount = g.Sum(x => x.Amount);
                }
                return allDays;

            default: // "daily"
                return rows.Where(r => r.PaidAt >= now.Date)
                    .GroupBy(r => r.PaidAt.Hour)
                    .Select(g => new IncomeChartPointDto { Label = $"{g.Key}:00", Amount = g.Sum(x => x.Amount) })
                    .OrderBy(p => p.Label)
                    .ToList();
        }
    }
}
