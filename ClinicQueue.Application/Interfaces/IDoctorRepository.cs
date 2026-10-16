using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface IDoctorRepository
{
    Task<List<DoctorDetailsResponse>> GetAllDetailsAsync(CancellationToken ct);
    Task<DoctorDetailsResponse?> GetDetailsByIdAsync(int id, CancellationToken ct);
    Task<Doctor?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> HasSlotsAsync(int id, CancellationToken ct);
    Task AddAsync(Doctor doctor, CancellationToken ct);
    void Remove(Doctor doctor);
}
