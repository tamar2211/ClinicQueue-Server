namespace ClinicQueue.Infrastructure.Security;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    // סוד - מגיע מ-User Secrets / משתנה סביבה, לא מ-appsettings.json
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
