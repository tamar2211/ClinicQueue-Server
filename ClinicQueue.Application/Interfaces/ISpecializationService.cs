using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Doctors;

namespace ClinicQueue.Application.Interfaces;

public interface ISpecializationService
{
    Task<Result<List<SpecializationDetailsResponse>>> GetAllAsync(CancellationToken ct);
}
