using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _context;

    public DoctorRepository(AppDbContext context) => _context = context;

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

    // הקרנה לתוך ה-DB: שאילתה אחת עם התמחויות וספירת משבצות פנויות (בלי N+1)
    private IQueryable<DoctorDetailsResponse> DetailsQuery()
    {
        var now = DateTime.UtcNow;
        return _context.Doctors.AsNoTracking().Select(d => new DoctorDetailsResponse
        {
            Id = d.Id,
            FullName = d.FullName,
            IsActive = d.IsActive,
            Specializations = d.Specializations
                .Select(s => new SpecializationDetailsResponse { Id = s.Id, Name = s.Name })
                .ToList(),
            AvailableSlotsCount = d.Slots.Count(s => !s.IsBooked && s.StartTimeUtc > now)
        });
    }
}
