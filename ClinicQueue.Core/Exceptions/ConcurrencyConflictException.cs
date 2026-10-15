namespace ClinicQueue.Core.Exceptions;

/// <summary>
/// נזרקת כשמישהו אחר שינה את הרשומה בין הקריאה לשמירה (למשל: תפס את אותה משבצת).
///
/// למה חריגה משלנו ולא DbUpdateConcurrencyException ישירות?
/// DbUpdateConcurrencyException שייכת ל-EF Core. אם ה-Service היה תופס אותה,
/// הוא היה צריך להכיר את EF - וזה שובר את הפרדת השכבות.
/// לכן ה-Repository (ב-Data) תופס את DbUpdateConcurrencyException הספציפית,
/// וזורק במקומה את החריגה הזו, שמוגדרת ב-Core ומוכרת לכולם.
/// ה-Service תופס אותה ומחזיר Result.Conflict, וה-Controller מחזיר 409.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
