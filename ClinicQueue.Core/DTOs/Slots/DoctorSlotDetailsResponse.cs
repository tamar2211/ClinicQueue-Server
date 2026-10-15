namespace ClinicQueue.Core.DTOs.Slots;

public class DoctorSlotDetailsResponse
{
    public int Id { get; set; }

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    // ב-UTC. הלקוח ממיר לשעון ישראל בתצוגה
    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public bool IsBooked { get; set; }
}
