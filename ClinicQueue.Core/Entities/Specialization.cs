namespace ClinicQueue.Core.Entities;

/// <summary>
/// התמחות רפואית (למשל: רפואת משפחה, ילדים, עור).
/// </summary>
public class Specialization
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // many-to-many: התמחות אחת שייכת להרבה רופאים
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
