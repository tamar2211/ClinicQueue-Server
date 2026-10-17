using ClinicQueue.Application.Common.Exceptions;
using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ClinicQueue.Application.Services;

/// <summary>
/// לב הפרויקט: קביעת וביטול תורים על משאב מוגבל (DoctorSlot).
/// ההגנה מפני תחרות: Version על המשבצת (שתי מטופלות על אותה משבצת)
/// ו-Version על המשתמשת (שתי קביעות מקבילות של אותה מטופלת). ראו docs/concurrency-notes.md
/// </summary>
public class AppointmentService : IAppointmentService
{
    private const int MinHoursBeforeCancel = 24;

    private const string BookingConflict =
        "לא ניתן היה להשלים את קביעת התור: התור נתפס על ידי מטופל אחר, או שבקשה נוספת שלך מתבצעת כרגע. נסה שוב.";

    private const string CancelConflict = "לא ניתן היה לבטל את התור כי המצב שלו השתנה בינתיים. נסה שוב.";

    private readonly IAppointmentRepository _appointments;
    private readonly ISlotRepository _slots;
    private readonly IDoctorRepository _doctors;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IAppointmentRepository appointments, ISlotRepository slots, IDoctorRepository doctors,
        IUserRepository users, IUnitOfWork unitOfWork, ILogger<AppointmentService> logger)
    {
        _appointments = appointments;
        _slots = slots;
        _doctors = doctors;
        _users = users;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AppointmentDetailsResponse>> CreateAsync(
        int userId, CreateAppointmentRequest request, CancellationToken ct)
    {
        var slot = await _slots.GetByIdAsync(request.SlotId, ct);
        if (slot is null)
            return Result<AppointmentDetailsResponse>.NotFound("התור הפנוי לא נמצא");
        if (slot.IsBooked)
            return Result<AppointmentDetailsResponse>.Conflict("התור כבר נתפס");
        if (slot.StartTimeUtc <= DateTime.UtcNow)
            return Result<AppointmentDetailsResponse>.Validation("אי אפשר לקבוע תור שהזמן שלו עבר");

        var doctor = await _doctors.GetDetailsByIdAsync(slot.DoctorId, ct);
        if (doctor is not { IsActive: true })
            return Result<AppointmentDetailsResponse>.Validation("הרופא אינו פעיל");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AppointmentDetailsResponse>.Unauthorized("המשתמשת לא נמצאה");

        if (await _appointments.HasActiveAppointmentAtAsync(userId, slot.StartTimeUtc, slot.EndTimeUtc, ct))
            return Result<AppointmentDetailsResponse>.Conflict("כבר יש לך תור פעיל בזמן חופף");

        // הבדיקות למעלה והשמירה כאן הן טרנזקציה אחת: אם מישהו שינה את המשבצת או את המשתמשת
        // בינתיים, ה-Version שלהן כבר לא תואם והשמירה נכשלת
        slot.IsBooked = true;
        _users.Touch(user);
        var appointment = new Appointment { UserId = userId, DoctorSlotId = slot.Id };
        await _appointments.AddAsync(appointment, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "התנגשות בקביעת תור: משבצת {SlotId}, משתמשת {UserId}", slot.Id, userId);
            return Result<AppointmentDetailsResponse>.Conflict(BookingConflict);
        }

        _logger.LogInformation("תור {AppointmentId} נקבע: משבצת {SlotId}, משתמשת {UserId}", appointment.Id, slot.Id, userId);
        return Result<AppointmentDetailsResponse>.Success((await _appointments.GetDetailsByIdAsync(appointment.Id, ct))!);
    }

    public async Task<Result<AppointmentDetailsResponse>> GetByIdAsync(
        int id, int userId, bool isAdmin, CancellationToken ct)
    {
        var appointment = await _appointments.GetByIdAsync(id, ct);
        if (appointment is null)
            return Result<AppointmentDetailsResponse>.NotFound("התור לא נמצא");
        if (!isAdmin && appointment.UserId != userId)
            return Result<AppointmentDetailsResponse>.Forbidden("אפשר לצפות רק בתור שלך");

        return Result<AppointmentDetailsResponse>.Success((await _appointments.GetDetailsByIdAsync(id, ct))!);
    }

    public async Task<Result<PagedResponse<AppointmentDetailsResponse>>> GetMineAsync(
        int userId, PaginationQuery query, CancellationToken ct) =>
        Result<PagedResponse<AppointmentDetailsResponse>>.Success(
            await _appointments.GetByUserAsync(userId, query, ct));

    public async Task<Result<PagedResponse<AppointmentDetailsResponse>>> GetAllAsync(
        PaginationQuery query, CancellationToken ct) =>
        Result<PagedResponse<AppointmentDetailsResponse>>.Success(
            await _appointments.GetAllAsync(query, ct));

    public async Task<Result> CancelAsync(int id, int userId, bool isAdmin, CancellationToken ct)
    {
        var appointment = await _appointments.GetByIdAsync(id, ct);
        if (appointment is null) return Result.NotFound("התור לא נמצא");
        if (!isAdmin && appointment.UserId != userId) return Result.Forbidden("אפשר לבטל רק תור שלך");
        if (appointment.Status != AppointmentStatus.Active) return Result.Conflict("התור כבר בוטל");

        var now = DateTime.UtcNow;
        var slot = appointment.DoctorSlot;
        if (slot.StartTimeUtc <= now)
            return Result.Validation("אי אפשר לבטל תור שכבר התחיל");
        if (!isAdmin && slot.StartTimeUtc - now < TimeSpan.FromHours(MinHoursBeforeCancel))
            return Result.Validation($"אפשר לבטל תור עד {MinHoursBeforeCancel} שעות לפני מועדו");

        // ביטול = שינוי סטטוס (התור נשאר בהיסטוריה) והחזרת המשבצת לפנויה
        appointment.Status = isAdmin ? AppointmentStatus.CancelledByClinic : AppointmentStatus.CancelledByPatient;
        appointment.CancelledAtUtc = now;
        slot.IsBooked = false;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "התנגשות בביטול תור {AppointmentId}", id);
            return Result.Conflict(CancelConflict);
        }

        _logger.LogInformation("תור {AppointmentId} בוטל ({Status})", id, appointment.Status);
        return Result.Success();
    }
}
