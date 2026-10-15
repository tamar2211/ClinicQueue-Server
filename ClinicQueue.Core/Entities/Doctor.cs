namespace ClinicQueue.Core.Entities;

/// <summary>
/// רופא במרפאה.
/// </summary>
public class Doctor
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // רופא לא פעיל לא מופיע לקביעת תורים חדשים
    public bool IsActive { get; set; } = true;

    // many-to-many: לרופא יכולות להיות כמה התמחויות
    public ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();

    // one-to-many: לרופא יש הרבה משבצות זמן
    public ICollection<DoctorSlot> Slots { get; set; } = new List<DoctorSlot>();
}
