using ClinicQueue.API.Extensions;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Controllers;

[ApiController]
[Route("api/specializations")]
[Authorize]
public class SpecializationsController : ControllerBase
{
    private readonly ISpecializationService _specializations;

    public SpecializationsController(ISpecializationService specializations) => _specializations = specializations;

    [HttpGet]
    [ProducesResponseType(typeof(List<SpecializationDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(CancellationToken ct) =>
        (await _specializations.GetAllAsync(ct)).ToActionResult(this);
}
