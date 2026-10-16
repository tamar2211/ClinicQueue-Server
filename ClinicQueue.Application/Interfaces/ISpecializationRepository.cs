using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface ISpecializationRepository
{
    Task<List<Specialization>> GetAllAsync(CancellationToken ct);
    Task<List<Specialization>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
}
