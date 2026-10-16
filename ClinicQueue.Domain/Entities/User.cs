using ClinicQueue.Domain.Enums;

namespace ClinicQueue.Domain.Entities;

/// <summary>
/// משתמש במערכת - מטופל או מנהל.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // ייחודי - מוגדר כאינדקס UNIQUE ב-Fluent API בשכבת Data
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    // לעולם לא שומרים סיסמה גלויה - רק hash
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Patient;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Concurrency token של המשתמשת: קביעת תור "נוגעת" בשורה הזו, כך ששתי קביעות מקבילות
    // של אותה מטופלת (גם על משבצות שונות וחופפות) מתנגשות זו בזו. ההגדרה ב-Fluent API.
    public Guid Version { get; set; } = Guid.NewGuid();

    // one-to-many: למשתמש יכולים להיות הרבה תורים
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
