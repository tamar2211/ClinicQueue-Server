using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Core.DTOs.Auth;

/// <summary>
/// הרשמה של מטופל חדש. אין כאן שדה Role - כל מי שנרשם הוא Patient.
/// </summary>
public class UserRegisterRequest
{
    [Required(ErrorMessage = "שם מלא הוא שדה חובה")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם מלא חייב להכיל 2-100 תווים")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "אימייל הוא שדה חובה")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "טלפון הוא שדה חובה")]
    [RegularExpression(@"^0\d{1,2}-?\d{7}$", ErrorMessage = "מספר טלפון לא תקין (לדוגמה: 050-1234567)")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה היא שדה חובה")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להכיל לפחות 6 תווים")]
    public string Password { get; set; } = string.Empty;
}
