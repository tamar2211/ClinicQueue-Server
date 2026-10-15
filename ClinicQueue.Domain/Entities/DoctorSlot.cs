namespace ClinicQueue.Domain.Entities;

/// <summary>
/// משבצת זמן אצל רופא - זה המשאב המוגבל של המערכת.
/// כל משבצת יכולה להיות תפוסה על ידי מטופל אחד בלבד.
/// </summary>
public class DoctorSlot
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = default!;

    // כל הזמנים נשמרים ב-UTC (דרישה של PostgreSQL)
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public bool IsBooked { get; set; }

    // Concurrency token (גישה אופטימית):
    // EF מוסיף את הערך הישן לתנאי ה-WHERE של ה-UPDATE.
    // אם מטופל אחר הספיק לתפוס את המשבצת בינתיים - הערך כבר השתנה,
    // אף שורה לא מתעדכנת, ו-EF זורק DbUpdateConcurrencyException.
    // ההגדרה כ-concurrency token נמצאת ב-Fluent API (Infrastructure),
    // והערך מוחלף בכל שמירה בדריסה של SaveChangesAsync ב-AppDbContext.
    public Guid Version { get; set; } = Guid.NewGuid();

    // one-to-many: היסטוריית התורים על המשבצת (כולל מבוטלים).
    // לכל היותר תור אחד פעיל בכל רגע.
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
