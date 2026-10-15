using ClinicQueue.Core.DTOs.Common;

namespace ClinicQueue.Core.DTOs.Slots;

/// <summary>
/// סינון משבצות. כל הפרמטרים אופציונליים, מגיעים מה-URL:
/// GET /api/slots?specializationId=2&amp;date=2026-11-03&amp;onlyAvailable=true&amp;page=1
/// </summary>
public class SlotQueryParameters : PaginationQuery
{
    public int? DoctorId { get; set; }

    // סינון לפי התמחות - עובר דרך קשר ה-many-to-many של הרופא
    public int? SpecializationId { get; set; }

    // יום שלם (לפי שעון ישראל - ההמרה ל-UTC נעשית ב-Service)
    public DateOnly? Date { get; set; }

    public bool OnlyAvailable { get; set; } = true;
}
