using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Core.DTOs.Doctors;

/// <summary>
/// עדכון רופא (מנהל בלבד). IsActive=false משבית את הרופא -
/// ה-Service יחזיר 409 אם יש לו תורים עתידיים פעילים.
/// </summary>
public class UpdateDoctorRequest
{
    [Required(ErrorMessage = "שם הרופא הוא שדה חובה")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם הרופא חייב להכיל 2-100 תווים")]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [MinLength(1, ErrorMessage = "יש לבחור לפחות התמחות אחת")]
    public List<int> SpecializationIds { get; set; } = new();
}
