namespace ClinicQueue.Application.DTOs.Appointments;

public class AppointmentDetailsResponse
{
    public int Id { get; set; }

    public int SlotId { get; set; }

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    // שם המטופל - שימושי למסך המנהל
    public string PatientName { get; set; } = string.Empty;

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    // "Active" / "CancelledByPatient" / "CancelledByClinic"
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
