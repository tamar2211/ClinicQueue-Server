using ClinicQueue.Domain.Enums;

namespace ClinicQueue.Domain.Entities;

/// <summary>
/// תור - מטופל שתפס משבצת זמן אצל רופא.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int DoctorSlotId { get; set; }
    public DoctorSlot DoctorSlot { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Active;

    // מתי בוטל (null כל עוד התור פעיל)
    public DateTime? CancelledAtUtc { get; set; }
}
