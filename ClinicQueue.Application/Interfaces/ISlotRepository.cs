using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface ISlotRepository
{
    Task<PagedResponse<DoctorSlotDetailsResponse>> SearchAsync(SlotQueryParameters query, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct);
    Task<DoctorSlotDetailsResponse?> GetDetailsByIdAsync(int id, CancellationToken ct);
    Task<DoctorSlot?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> HasOverlapAsync(int doctorId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task AddAsync(DoctorSlot slot, CancellationToken ct);
    void Remove(DoctorSlot slot);
}
