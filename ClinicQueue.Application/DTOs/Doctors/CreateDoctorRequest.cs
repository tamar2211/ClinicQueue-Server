using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Application.DTOs.Doctors;

/// <summary>
/// הוספת רופא (מנהל בלבד). רופא חדש תמיד נוצר כפעיל.
/// </summary>
public class CreateDoctorRequest
{
    [Required(ErrorMessage = "שם הרופא הוא שדה חובה")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם הרופא חייב להכיל 2-100 תווים")]
    public string FullName { get; set; } = string.Empty;

    // מזהי ההתמחויות. הבדיקה שהן קיימות - ב-Service (תלויה בנתונים)
    [MinLength(1, ErrorMessage = "יש לבחור לפחות התמחות אחת")]
    public List<int> SpecializationIds { get; set; } = new();
}
