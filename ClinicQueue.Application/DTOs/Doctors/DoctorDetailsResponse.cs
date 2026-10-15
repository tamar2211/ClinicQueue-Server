namespace ClinicQueue.Application.DTOs.Doctors;

public class DoctorDetailsResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<SpecializationDetailsResponse> Specializations { get; set; } = new();

    // כמה משבצות עתידיות פנויות יש לרופא.
    // מחושב בשאילתה אחת לכל הרופאים יחד (בלי N+1)
    public int AvailableSlotsCount { get; set; }
}
