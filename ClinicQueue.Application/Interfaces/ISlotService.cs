using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.DTOs.Slots;

namespace ClinicQueue.Application.Interfaces;

public interface ISlotService
{
    Task<Result<PagedResponse<DoctorSlotDetailsResponse>>> SearchAsync(SlotQueryParameters query, CancellationToken ct);
    Task<Result<DoctorSlotDetailsResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<DoctorSlotDetailsResponse>> CreateAsync(CreateDoctorSlotRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
