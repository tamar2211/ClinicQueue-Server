using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public AppointmentRepository(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<Appointment?> GetByIdAsync(int id, CancellationToken ct) =>
        _context.Appointments.Include(a => a.DoctorSlot).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<AppointmentDetailsResponse?> GetDetailsByIdAsync(int id, CancellationToken ct) =>
        _context.Appointments.AsNoTracking().Where(a => a.Id == id)
            .ProjectTo<AppointmentDetailsResponse>(_mapper.ConfigurationProvider).FirstOrDefaultAsync(ct);

    public Task<PagedResponse<AppointmentDetailsResponse>> GetByUserAsync(
        int userId, PaginationQuery query, CancellationToken ct) =>
        PageAsync(_context.Appointments.Where(a => a.UserId == userId), query, ct);

    public Task<PagedResponse<AppointmentDetailsResponse>> GetAllAsync(PaginationQuery query, CancellationToken ct) =>
        PageAsync(_context.Appointments, query, ct);

    public Task<bool> HasActiveAppointmentAtAsync(int userId, DateTime startUtc, DateTime endUtc, CancellationToken ct) =>
        _context.Appointments.AnyAsync(a => a.UserId == userId
            && a.Status == AppointmentStatus.Active
            && a.DoctorSlot.StartTimeUtc < endUtc
            && a.DoctorSlot.EndTimeUtc > startUtc, ct);

    public Task<bool> HasFutureActiveAppointmentsAsync(int doctorId, DateTime nowUtc, CancellationToken ct) =>
        _context.Appointments.AnyAsync(a => a.Status == AppointmentStatus.Active
            && a.DoctorSlot.DoctorId == doctorId
            && a.DoctorSlot.StartTimeUtc > nowUtc, ct);

    public async Task AddAsync(Appointment appointment, CancellationToken ct) =>
        await _context.Appointments.AddAsync(appointment, ct);

    private async Task<PagedResponse<AppointmentDetailsResponse>> PageAsync(
        IQueryable<Appointment> appointments, PaginationQuery query, CancellationToken ct)
    {
        appointments = appointments.AsNoTracking();
        var totalCount = await appointments.CountAsync(ct);
        var items = await appointments
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<AppointmentDetailsResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResponse<AppointmentDetailsResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}
