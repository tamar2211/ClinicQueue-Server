namespace ClinicQueue.Core.DTOs.Auth;

/// <summary>
/// מוחזר אחרי הרשמה או התחברות מוצלחת - למטופלת וגם למנהלת. הלקוח שומר את ה-Token ושולח אותו ב-header.
/// </summary>
public class UserLoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
