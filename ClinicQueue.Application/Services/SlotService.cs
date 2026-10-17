using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Services;

public class SlotService : ISlotService
{
    // ה-id של IANA עובד גם ב-Linux (Render) וגם ב-Windows
    private static readonly TimeZoneInfo IsraelTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    private readonly ISlotRepository _slots;
    private readonly IDoctorRepository _doctors;
    private readonly IUnitOfWork _unitOfWork;

    public SlotService(ISlotRepository slots, IDoctorRepository doctors, IUnitOfWork unitOfWork)
    {
        _slots = slots;
        _doctors = doctors;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResponse<DoctorSlotDetailsResponse>>> SearchAsync(
        SlotQueryParameters query, CancellationToken ct)
    {
        // יום שלם לפי שעון ישראל ← טווח UTC
        DateTime? fromUtc = null, toUtc = null;
        if (query.Date is { } date)
        {
            fromUtc = IsraelMidnightToUtc(date);
            toUtc = IsraelMidnightToUtc(date.AddDays(1));
        }

        return Result<PagedResponse<DoctorSlotDetailsResponse>>.Success(
            await _slots.SearchAsync(query, fromUtc, toUtc, ct));
    }

    public async Task<Result<DoctorSlotDetailsResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var slot = await _slots.GetDetailsByIdAsync(id, ct);
        return slot is null
            ? Result<DoctorSlotDetailsResponse>.NotFound("התור הפנוי לא נמצא")
            : Result<DoctorSlotDetailsResponse>.Success(slot);
    }

    public async Task<Result<DoctorSlotDetailsResponse>> CreateAsync(CreateDoctorSlotRequest request, CancellationToken ct)
    {
        var start = NormalizeToUtc(request.StartTimeUtc!.Value);
        var end = NormalizeToUtc(request.EndTimeUtc!.Value);

        if (end <= start)
            return Result<DoctorSlotDetailsResponse>.Validation("שעת הסיום חייבת להיות אחרי שעת ההתחלה");
        if (start <= DateTime.UtcNow)
            return Result<DoctorSlotDetailsResponse>.Validation("אפשר ליצור תור פנוי רק בזמן עתידי");

        var doctor = await _doctors.GetDetailsByIdAsync(request.DoctorId, ct);
        if (doctor is null)
            return Result<DoctorSlotDetailsResponse>.NotFound("הרופא לא נמצא");
        if (!doctor.IsActive)
            return Result<DoctorSlotDetailsResponse>.Validation("אי אפשר ליצור תור פנוי לרופא שאינו פעיל");

        if (await _slots.HasOverlapAsync(request.DoctorId, start, end, ct))
            return Result<DoctorSlotDetailsResponse>.Conflict("קיים תור פנוי חופף אצל הרופא בזמן הזה");

        var slot = new DoctorSlot { DoctorId = request.DoctorId, StartTimeUtc = start, EndTimeUtc = end };
        await _slots.AddAsync(slot, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<DoctorSlotDetailsResponse>.Success((await _slots.GetDetailsByIdAsync(slot.Id, ct))!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var slot = await _slots.GetByIdAsync(id, ct);
        if (slot is null) return Result.NotFound("התור הפנוי לא נמצא");
        if (slot.IsBooked) return Result.Conflict("אי אפשר למחוק תור שנתפס");

        _slots.Remove(slot);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static DateTime IsraelMidnightToUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), IsraelTime);

    // Npgsql דורש Kind=Utc: זמן בלי סימון נחשב UTC, וזמן מקומי מומר
    private static DateTime NormalizeToUtc(DateTime time) => time.Kind switch
    {
        DateTimeKind.Utc => time,
        DateTimeKind.Local => time.ToUniversalTime(),
        _ => DateTime.SpecifyKind(time, DateTimeKind.Utc)
    };
}
