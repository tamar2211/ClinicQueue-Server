using AutoMapper;
using ClinicQueue.Application.DTOs.Auth;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // האימייל, הסיסמה (hash) והתפקיד נקבעים ב-AuthService
        CreateMap<UserRegisterRequest, User>()
            .ForMember(u => u.Id, o => o.Ignore())
            .ForMember(u => u.PasswordHash, o => o.Ignore())
            .ForMember(u => u.Role, o => o.Ignore())
            .ForMember(u => u.CreatedAtUtc, o => o.Ignore())
            .ForMember(u => u.Appointments, o => o.Ignore());
    }
}
