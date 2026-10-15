using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Application.DTOs.Auth;

public class UserLoginRequest
{
    [Required(ErrorMessage = "אימייל הוא שדה חובה")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה היא שדה חובה")]
    public string Password { get; set; } = string.Empty;
}
