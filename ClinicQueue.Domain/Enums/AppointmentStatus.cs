namespace ClinicQueue.Domain.Enums;

/// <summary>
/// מצב התור. תור מבוטל לא נמחק - הוא נשאר בהיסטוריה עם סטטוס ביטול.
/// </summary>
public enum AppointmentStatus
{
    Active = 0,
    CancelledByPatient = 1,
    CancelledByClinic = 2
}
