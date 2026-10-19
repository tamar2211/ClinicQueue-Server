using AutoMapper;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Auth;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Mapping;

/// <summary>
/// כל המיפויים בין ישויות ל-DTOs. ה-repositories משתמשים בהם דרך ProjectTo,
/// שהופך את המיפוי לשאילתת SQL אחת (רק העמודות הנדרשות, בלי N+1).
/// שדות עם אותו שם ממופים אוטומטית; כאן כתוב רק מה ששונה.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Specialization, SpecializationDetailsResponse>();

        // now מועבר כפרמטר בזמן השאילתה: ProjectTo(..., new { now = DateTime.UtcNow })
        DateTime now = default;
        CreateMap<Doctor, DoctorDetailsResponse>()
            .ForMember(d => d.AvailableSlotsCount,
                o => o.MapFrom(doc => doc.Slots.Count(s => !s.IsBooked && s.StartTimeUtc > now)));

        CreateMap<DoctorSlot, DoctorSlotDetailsResponse>()
            .ForMember(d => d.DoctorName, o => o.MapFrom(s => s.Doctor.FullName));

        CreateMap<Appointment, AppointmentDetailsResponse>()
            .ForMember(d => d.SlotId, o => o.MapFrom(a => a.DoctorSlotId))
            .ForMember(d => d.DoctorId, o => o.MapFrom(a => a.DoctorSlot.DoctorId))
            .ForMember(d => d.DoctorName, o => o.MapFrom(a => a.DoctorSlot.Doctor.FullName))
            .ForMember(d => d.PatientName, o => o.MapFrom(a => a.User.FullName))
            .ForMember(d => d.StartTimeUtc, o => o.MapFrom(a => a.DoctorSlot.StartTimeUtc))
            .ForMember(d => d.EndTimeUtc, o => o.MapFrom(a => a.DoctorSlot.EndTimeUtc))
            .ForMember(d => d.Status, o => o.MapFrom(a => a.Status.ToString()));

        // האימייל, הסיסמה (hash) והתפקיד נקבעים ב-AuthService
        CreateMap<UserRegisterRequest, User>()
            .ForMember(u => u.Id, o => o.Ignore())
            .ForMember(u => u.PasswordHash, o => o.Ignore())
            .ForMember(u => u.Role, o => o.Ignore())
            .ForMember(u => u.Version, o => o.Ignore())
            .ForMember(u => u.CreatedAtUtc, o => o.Ignore())
            .ForMember(u => u.Appointments, o => o.Ignore());
    }
}
