using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for expert proposals/quotes on requests.
/// </summary>
public interface IProposalService
{
    Task<PagedResult<ProposalDto>> GetPagedAsync(ProposalFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProposalDto>> GetByRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProposalDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default);
    Task<ProposalDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProposalDto> CreateAsync(CreateProposalDto dto, Guid expertId, CancellationToken cancellationToken = default);
    Task<ProposalDto?> UpdateAsync(int id, UpdateProposalDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, ProposalStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a proposal as the customer: rejects siblings, marks the request Booked
    /// and records AcceptedProposalId. Returns true on success.
    /// </summary>
    Task<bool> AcceptAsync(int proposalId, Guid customerUserId, CancellationToken cancellationToken = default);
}
