using ClinicQueue.API.Extensions;
using ClinicQueue.Application.DTOs.Doctors;
using ClinicQueue.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctors;

    public DoctorsController(IDoctorService doctors) => _doctors = doctors;

    [HttpGet]
    [ProducesResponseType(typeof(List<DoctorDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(CancellationToken ct) =>
        (await _doctors.GetAllAsync(ct)).ToActionResult(this);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DoctorDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        (await _doctors.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DoctorDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateDoctorRequest request, CancellationToken ct)
    {
        var result = await _doctors.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update([FromRoute] int id, [FromBody] UpdateDoctorRequest request, CancellationToken ct) =>
        (await _doctors.UpdateAsync(id, request, ct)).ToActionResult(this);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        (await _doctors.DeleteAsync(id, ct)).ToActionResult(this);
}
