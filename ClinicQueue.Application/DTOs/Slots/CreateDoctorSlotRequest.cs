using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Application.DTOs.Slots;

/// <summary>
/// הוספת משבצת בודדת (מנהל בלבד). אורך המשבצת חופשי.
/// ולידציות עסקיות ב-Service: סיום אחרי התחלה ורק בעתיד (400),
/// רופא קיים ופעיל (404/400), אין חפיפה עם משבצת אחרת של אותו רופא (409).
/// </summary>
public class CreateDoctorSlotRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "יש לבחור רופא")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "שעת התחלה היא שדה חובה")]
    public DateTime? StartTimeUtc { get; set; }

    [Required(ErrorMessage = "שעת סיום היא שדה חובה")]
    public DateTime? EndTimeUtc { get; set; }
}
