using ClinicQueue.Application.Common.Exceptions;
using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Application.Services;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClinicQueue.Tests;

/// <summary>
/// בדיקות על המשאב המוגבל: קביעת תור על משבצת (תקין ונדחה) וביטול.
/// ה-Repositories מדומים ב-Moq, ולכן נבדקת רק הלוגיקה של ה-Service.
/// </summary>
public class AppointmentServiceTests
{
    private const int UserId = 5;
    private const int OtherUserId = 6;
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<ISlotRepository> _slots = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<AppointmentService>> _logger = new();
    private readonly AppointmentService _sut;

    private readonly DoctorSlot _slot = new()
    {
        Id = 1,
        DoctorId = 7,
        StartTimeUtc = DateTime.UtcNow.AddDays(3),
        EndTimeUtc = DateTime.UtcNow.AddDays(3).AddMinutes(30)
    };

    private readonly User _user = new() { Id = UserId };

    // ברירת המחדל היא מסלול תקין: משבצת פנויה, רופא פעיל, אין חפיפה
    public AppointmentServiceTests()
    {
        _slots.Setup(s => s.GetByIdAsync(1, Ct)).ReturnsAsync(_slot);
        _doctors.Setup(d => d.GetDetailsByIdAsync(7, Ct)).ReturnsAsync(new DoctorDetailsResponse { Id = 7, IsActive = true });
        _users.Setup(u => u.GetByIdAsync(UserId, Ct)).ReturnsAsync(_user);
        _appointments.Setup(a => a.HasActiveAppointmentAtAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), Ct)).ReturnsAsync(false);
        _appointments.Setup(a => a.GetDetailsByIdAsync(It.IsAny<int>(), Ct)).ReturnsAsync(new AppointmentDetailsResponse { Id = 1 });

        _sut = new AppointmentService(_appointments.Object, _slots.Object, _doctors.Object, _users.Object,
            _unitOfWork.Object, _logger.Object);
    }

    // ---------- קביעת תור ----------

    [Fact]
    public async Task Create_FreeSlot_BooksSlotAndCreatesAppointment()
    {
        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.True(result.IsSuccess);
        Assert.True(_slot.IsBooked);
        _appointments.Verify(a => a.AddAsync(It.Is<Appointment>(x => x.UserId == UserId && x.DoctorSlotId == 1), Ct), Times.Once);
        _users.Verify(u => u.Touch(_user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Create_AlreadyBookedSlot_ReturnsConflictAndSavesNothing()
    {
        _slot.IsBooked = true;

        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _appointments.Verify(a => a.AddAsync(It.IsAny<Appointment>(), Ct), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Create_SlotNotFound_ReturnsNotFound()
    {
        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 99 }, Ct);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Create_SlotInThePast_ReturnsValidation()
    {
        _slot.StartTimeUtc = DateTime.UtcNow.AddHours(-1);

        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_InactiveDoctor_ReturnsValidation()
    {
        _doctors.Setup(d => d.GetDetailsByIdAsync(7, Ct)).ReturnsAsync(new DoctorDetailsResponse { Id = 7, IsActive = false });

        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_PatientAlreadyHasOverlappingAppointment_ReturnsConflict()
    {
        _appointments.Setup(a => a.HasActiveAppointmentAtAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), Ct)).ReturnsAsync(true);

        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Create_SomeoneElseTookTheSlotMeanwhile_ReturnsConflictAndLogsWarning()
    {
        // התחרות עצמה: ב-DB ה-Version כבר השתנה, ה-UnitOfWork ממיר את החריגה
        _unitOfWork.Setup(u => u.SaveChangesAsync(Ct)).ThrowsAsync(new ConcurrencyConflictException("conflict"));

        var result = await _sut.CreateAsync(UserId, new CreateAppointmentRequest { SlotId = 1 }, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        VerifyLogged(LogLevel.Warning, Times.Once());
    }

    // ---------- ביטול תור ----------

    [Fact]
    public async Task Cancel_ByOwner_MarksCancelledByPatientAndFreesSlot()
    {
        var appointment = ActiveAppointment(UserId, DateTime.UtcNow.AddDays(3));

        var result = await _sut.CancelAsync(10, UserId, isAdmin: false, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.CancelledByPatient, appointment.Status);
        Assert.NotNull(appointment.CancelledAtUtc);
        Assert.False(appointment.DoctorSlot.IsBooked);
    }

    [Fact]
    public async Task Cancel_AnotherPatientsAppointment_ReturnsForbidden()
    {
        var appointment = ActiveAppointment(UserId, DateTime.UtcNow.AddDays(3));

        var result = await _sut.CancelAsync(10, OtherUserId, isAdmin: false, Ct);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(AppointmentStatus.Active, appointment.Status);
    }

    [Fact]
    public async Task Cancel_PatientLessThan24HoursBefore_ReturnsValidation()
    {
        ActiveAppointment(UserId, DateTime.UtcNow.AddHours(5));

        var result = await _sut.CancelAsync(10, UserId, isAdmin: false, Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_AdminLessThan24HoursBefore_IsAllowedAndMarkedByClinic()
    {
        var appointment = ActiveAppointment(UserId, DateTime.UtcNow.AddHours(5));

        var result = await _sut.CancelAsync(10, OtherUserId, isAdmin: true, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.CancelledByClinic, appointment.Status);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsConflict()
    {
        var appointment = ActiveAppointment(UserId, DateTime.UtcNow.AddDays(3));
        appointment.Status = AppointmentStatus.CancelledByPatient;

        var result = await _sut.CancelAsync(10, UserId, isAdmin: false, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    private Appointment ActiveAppointment(int ownerId, DateTime slotStart)
    {
        var appointment = new Appointment
        {
            Id = 10,
            UserId = ownerId,
            Status = AppointmentStatus.Active,
            DoctorSlot = new DoctorSlot
            {
                Id = 1,
                IsBooked = true,
                StartTimeUtc = slotStart,
                EndTimeUtc = slotStart.AddMinutes(30)
            }
        };
        _appointments.Setup(a => a.GetByIdAsync(10, Ct)).ReturnsAsync(appointment);
        return appointment;
    }

    private void VerifyLogged(LogLevel level, Times times) =>
        _logger.Verify(l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
}
