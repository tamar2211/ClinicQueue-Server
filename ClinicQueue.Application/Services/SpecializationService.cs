using AutoMapper;
using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;

namespace ClinicQueue.Application.Services;

public class SpecializationService : ISpecializationService
{
    private readonly ISpecializationRepository _specializations;
    private readonly IMapper _mapper;

    public SpecializationService(ISpecializationRepository specializations, IMapper mapper)
    {
        _specializations = specializations;
        _mapper = mapper;
    }

    public async Task<Result<List<SpecializationDetailsResponse>>> GetAllAsync(CancellationToken ct) =>
        Result<List<SpecializationDetailsResponse>>.Success(
            _mapper.Map<List<SpecializationDetailsResponse>>(await _specializations.GetAllAsync(ct)));
}
