using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class SpecializationRepository : ISpecializationRepository
{
    private readonly AppDbContext _context;

    public SpecializationRepository(AppDbContext context) => _context = context;

    public Task<List<Specialization>> GetAllAsync(CancellationToken ct) =>
        _context.Specializations.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<List<Specialization>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct) =>
        _context.Specializations.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
}
