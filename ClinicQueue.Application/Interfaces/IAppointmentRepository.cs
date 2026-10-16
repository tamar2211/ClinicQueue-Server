using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResponse<AppointmentDetailsResponse>> GetByUserAsync(int userId, PaginationQuery query, CancellationToken ct);
    Task<PagedResponse<AppointmentDetailsResponse>> GetAllAsync(PaginationQuery query, CancellationToken ct);
    Task<bool> HasActiveAppointmentAtAsync(int userId, DateTime startUtc, DateTime endUtc, CancellationToken ct);
    Task AddAsync(Appointment appointment, CancellationToken ct);
}
