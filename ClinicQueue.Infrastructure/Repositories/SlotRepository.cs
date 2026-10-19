using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class SlotRepository : ISlotRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SlotRepository(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponse<DoctorSlotDetailsResponse>> SearchAsync(
        SlotQueryParameters query, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var slots = _context.DoctorSlots.AsNoTracking().AsQueryable();

        if (query.DoctorId is not null)
            slots = slots.Where(s => s.DoctorId == query.DoctorId);
        if (query.SpecializationId is not null)
            slots = slots.Where(s => s.Doctor.Specializations.Any(sp => sp.Id == query.SpecializationId));
        if (fromUtc is not null)
            slots = slots.Where(s => s.StartTimeUtc >= fromUtc);
        if (toUtc is not null)
            slots = slots.Where(s => s.StartTimeUtc < toUtc);
        if (query.OnlyAvailable)
        {
            var now = DateTime.UtcNow;
            slots = slots.Where(s => !s.IsBooked && s.StartTimeUtc > now);
        }

        var totalCount = await slots.CountAsync(ct);
        var items = await slots
            .OrderBy(s => s.StartTimeUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<DoctorSlotDetailsResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResponse<DoctorSlotDetailsResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<DoctorSlotDetailsResponse?> GetDetailsByIdAsync(int id, CancellationToken ct) =>
        _context.DoctorSlots.AsNoTracking().Where(s => s.Id == id)
            .ProjectTo<DoctorSlotDetailsResponse>(_mapper.ConfigurationProvider).FirstOrDefaultAsync(ct);

    public Task<DoctorSlot?> GetByIdAsync(int id, CancellationToken ct) =>
        _context.DoctorSlots.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> HasOverlapAsync(int doctorId, DateTime startUtc, DateTime endUtc, CancellationToken ct) =>
        _context.DoctorSlots.AnyAsync(
            s => s.DoctorId == doctorId && s.StartTimeUtc < endUtc && s.EndTimeUtc > startUtc, ct);

    public async Task AddAsync(DoctorSlot slot, CancellationToken ct) =>
        await _context.DoctorSlots.AddAsync(slot, ct);

    public void Remove(DoctorSlot slot) => _context.DoctorSlots.Remove(slot);
}
