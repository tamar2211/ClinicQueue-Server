namespace ClinicQueue.Domain.Enums;

/// <summary>
/// תפקיד המשתמש במערכת.
/// Patient - מטופל שקובע תורים לעצמו.
/// Admin - מנהל/מזכירה: מנהל רופאים, סלוטים ותורים של כולם.
/// </summary>
public enum UserRole
{
    Patient = 0,
    Admin = 1
}
