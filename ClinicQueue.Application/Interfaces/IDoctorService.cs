using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Doctors;

namespace ClinicQueue.Application.Interfaces;

public interface IDoctorService
{
    Task<Result<List<DoctorDetailsResponse>>> GetAllAsync(CancellationToken ct);
    Task<Result<DoctorDetailsResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<DoctorDetailsResponse>> CreateAsync(CreateDoctorRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateDoctorRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
