using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Application.Services;
using ClinicQueue.Domain.Entities;
using Moq;

namespace ClinicQueue.Tests;

public class DoctorServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<ISpecializationRepository> _specializations = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DoctorService _sut;

    public DoctorServiceTests() =>
        _sut = new DoctorService(_doctors.Object, _specializations.Object, _appointments.Object, _unitOfWork.Object);

    [Fact]
    public async Task Create_UnknownSpecialization_ReturnsValidation()
    {
        _specializations.Setup(s => s.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), Ct)).ReturnsAsync(new List<Specialization>());

        var result = await _sut.CreateAsync(new CreateDoctorRequest { FullName = "Dr X", SpecializationIds = { 99 } }, Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Update_DeactivateDoctorWithFutureAppointments_ReturnsConflict()
    {
        _doctors.Setup(d => d.GetByIdAsync(1, Ct)).ReturnsAsync(new Doctor { Id = 1, IsActive = true });
        _specializations.Setup(s => s.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), Ct))
            .ReturnsAsync(new List<Specialization> { new() { Id = 1 } });
        _appointments.Setup(a => a.HasFutureActiveAppointmentsAsync(1, It.IsAny<DateTime>(), Ct)).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(1, new UpdateDoctorRequest { FullName = "Dr X", IsActive = false, SpecializationIds = { 1 } }, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Delete_DoctorWithSlots_ReturnsConflict()
    {
        _doctors.Setup(d => d.GetByIdAsync(1, Ct)).ReturnsAsync(new Doctor { Id = 1 });
        _doctors.Setup(d => d.HasSlotsAsync(1, Ct)).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(1, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _doctors.Verify(d => d.Remove(It.IsAny<Doctor>()), Times.Never);
    }
}

public class SlotServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly Mock<ISlotRepository> _slots = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SlotService _sut;

    public SlotServiceTests()
    {
        _doctors.Setup(d => d.GetDetailsByIdAsync(1, Ct)).ReturnsAsync(new DoctorDetailsResponse { Id = 1, IsActive = true });
        _slots.Setup(s => s.GetDetailsByIdAsync(It.IsAny<int>(), Ct)).ReturnsAsync(new DoctorSlotDetailsResponse());
        _sut = new SlotService(_slots.Object, _doctors.Object, _unitOfWork.Object);
    }

    private static CreateDoctorSlotRequest Request(DateTime start, DateTime end) =>
        new() { DoctorId = 1, StartTimeUtc = start, EndTimeUtc = end };

    [Fact]
    public async Task Create_ValidFutureSlot_IsSaved()
    {
        var start = DateTime.UtcNow.AddDays(2);

        var result = await _sut.CreateAsync(Request(start, start.AddMinutes(30)), Ct);

        Assert.True(result.IsSuccess);
        _slots.Verify(s => s.AddAsync(It.IsAny<DoctorSlot>(), Ct), Times.Once);
    }

    [Fact]
    public async Task Create_TimeWithoutKind_IsTreatedAsUtc()
    {
        var start = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Unspecified);

        await _sut.CreateAsync(Request(start, start.AddMinutes(30)), Ct);

        _slots.Verify(s => s.AddAsync(It.Is<DoctorSlot>(x => x.StartTimeUtc.Kind == DateTimeKind.Utc), Ct), Times.Once);
    }

    [Fact]
    public async Task Create_EndBeforeStart_ReturnsValidation()
    {
        var start = DateTime.UtcNow.AddDays(2);

        var result = await _sut.CreateAsync(Request(start, start.AddMinutes(-30)), Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_InThePast_ReturnsValidation()
    {
        var start = DateTime.UtcNow.AddDays(-1);

        var result = await _sut.CreateAsync(Request(start, start.AddMinutes(30)), Ct);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_OverlappingSlot_ReturnsConflict()
    {
        _slots.Setup(s => s.HasOverlapAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), Ct)).ReturnsAsync(true);
        var start = DateTime.UtcNow.AddDays(2);

        var result = await _sut.CreateAsync(Request(start, start.AddMinutes(30)), Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Delete_BookedSlot_ReturnsConflict()
    {
        _slots.Setup(s => s.GetByIdAsync(1, Ct)).ReturnsAsync(new DoctorSlot { Id = 1, IsBooked = true });

        var result = await _sut.DeleteAsync(1, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _slots.Verify(s => s.Remove(It.IsAny<DoctorSlot>()), Times.Never);
    }
}
