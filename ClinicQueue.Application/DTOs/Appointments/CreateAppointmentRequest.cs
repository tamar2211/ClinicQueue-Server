using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Application.DTOs.Appointments;

/// <summary>
/// קביעת תור. רק מזהה המשבצת - זהות המטופל נלקחת מה-JWT,
/// כדי שאי אפשר יהיה לקבוע תור בשם מישהו אחר.
/// </summary>
public class CreateAppointmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "יש לבחור תור")]
    public int SlotId { get; set; }
}
