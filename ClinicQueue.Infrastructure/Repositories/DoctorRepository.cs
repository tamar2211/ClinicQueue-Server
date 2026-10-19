using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public DoctorRepository(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<DoctorDetailsResponse>> GetAllDetailsAsync(CancellationToken ct) =>
        DetailsQuery().OrderBy(d => d.FullName).ToListAsync(ct);

    public Task<DoctorDetailsResponse?> GetDetailsByIdAsync(int id, CancellationToken ct) =>
        DetailsQuery().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Doctor?> GetByIdAsync(int id, CancellationToken ct) =>
        _context.Doctors.Include(d => d.Specializations).FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> HasSlotsAsync(int id, CancellationToken ct) =>
        _context.DoctorSlots.AnyAsync(s => s.DoctorId == id, ct);

    public async Task AddAsync(Doctor doctor, CancellationToken ct) =>
        await _context.Doctors.AddAsync(doctor, ct);

    public void Remove(Doctor doctor) => _context.Doctors.Remove(doctor);

    // ProjectTo לפי MappingProfile: שאילתה אחת עם התמחויות וספירת משבצות פנויות (בלי N+1)
    private IQueryable<DoctorDetailsResponse> DetailsQuery() =>
        _context.Doctors.AsNoTracking()
            .ProjectTo<DoctorDetailsResponse>(_mapper.ConfigurationProvider, new { now = DateTime.UtcNow });
}
