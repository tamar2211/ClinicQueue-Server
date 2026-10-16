using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Common;

namespace ClinicQueue.Application.Interfaces;

public interface IAppointmentService
{
    Task<Result<AppointmentDetailsResponse>> CreateAsync(int userId, CreateAppointmentRequest request, CancellationToken ct);
    Task<Result<AppointmentDetailsResponse>> GetByIdAsync(int id, int userId, bool isAdmin, CancellationToken ct);
    Task<Result<PagedResponse<AppointmentDetailsResponse>>> GetMineAsync(int userId, PaginationQuery query, CancellationToken ct);
    Task<Result<PagedResponse<AppointmentDetailsResponse>>> GetAllAsync(PaginationQuery query, CancellationToken ct);
    Task<Result> CancelAsync(int id, int userId, bool isAdmin, CancellationToken ct);
}
