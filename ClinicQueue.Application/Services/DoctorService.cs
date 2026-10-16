using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Services;

public class DoctorService : IDoctorService
{
    private const string MissingSpecialization = "אחת ההתמחויות שנבחרו לא קיימת";

    private readonly IDoctorRepository _doctors;
    private readonly ISpecializationRepository _specializations;
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;

    public DoctorService(IDoctorRepository doctors, ISpecializationRepository specializations,
        IAppointmentRepository appointments, IUnitOfWork unitOfWork)
    {
        _doctors = doctors;
        _specializations = specializations;
        _appointments = appointments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<DoctorDetailsResponse>>> GetAllAsync(CancellationToken ct) =>
        Result<List<DoctorDetailsResponse>>.Success(await _doctors.GetAllDetailsAsync(ct));

    public async Task<Result<DoctorDetailsResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var doctor = await _doctors.GetDetailsByIdAsync(id, ct);
        return doctor is null
            ? Result<DoctorDetailsResponse>.NotFound("הרופא לא נמצא")
            : Result<DoctorDetailsResponse>.Success(doctor);
    }

    public async Task<Result<DoctorDetailsResponse>> CreateAsync(CreateDoctorRequest request, CancellationToken ct)
    {
        var specializations = await FindSpecializationsAsync(request.SpecializationIds, ct);
        if (specializations is null)
            return Result<DoctorDetailsResponse>.Validation(MissingSpecialization);

        var doctor = new Doctor { FullName = request.FullName.Trim(), Specializations = specializations };
        await _doctors.AddAsync(doctor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<DoctorDetailsResponse>.Success((await _doctors.GetDetailsByIdAsync(doctor.Id, ct))!);
    }

    public async Task<Result> UpdateAsync(int id, UpdateDoctorRequest request, CancellationToken ct)
    {
        var doctor = await _doctors.GetByIdAsync(id, ct);
        if (doctor is null) return Result.NotFound("הרופא לא נמצא");

        var specializations = await FindSpecializationsAsync(request.SpecializationIds, ct);
        if (specializations is null) return Result.Validation(MissingSpecialization);

        if (doctor.IsActive && !request.IsActive
            && await _appointments.HasFutureActiveAppointmentsAsync(id, DateTime.UtcNow, ct))
            return Result.Conflict("אי אפשר להשבית רופא שיש לו תורים עתידיים פעילים");

        doctor.FullName = request.FullName.Trim();
        doctor.IsActive = request.IsActive;
        doctor.Specializations.Clear();
        foreach (var specialization in specializations)
            doctor.Specializations.Add(specialization);

        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var doctor = await _doctors.GetByIdAsync(id, ct);
        if (doctor is null) return Result.NotFound("הרופא לא נמצא");

        if (await _doctors.HasSlotsAsync(id, ct))
            return Result.Conflict("אי אפשר למחוק רופא שיש לו משבצות זמן. אפשר להשבית אותו במקום.");

        _doctors.Remove(doctor);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // מחזיר null אם אחת ההתמחויות לא קיימת
    private async Task<List<Specialization>?> FindSpecializationsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var distinctIds = ids.Distinct().ToList();
        var found = await _specializations.GetByIdsAsync(distinctIds, ct);
        return found.Count == distinctIds.Count ? found : null;
    }
}
