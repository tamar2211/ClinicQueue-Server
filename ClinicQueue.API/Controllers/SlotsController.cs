using ClinicQueue.API.Extensions;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.DTOs.Slots;
using ClinicQueue.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Controllers;

[ApiController]
[Route("api/slots")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _slots;

    public SlotsController(ISlotService slots) => _slots = slots;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DoctorSlotDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Search([FromQuery] SlotQueryParameters query, CancellationToken ct) =>
        (await _slots.SearchAsync(query, ct)).ToActionResult(this);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DoctorSlotDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        (await _slots.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DoctorSlotDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create([FromBody] CreateDoctorSlotRequest request, CancellationToken ct)
    {
        var result = await _slots.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        (await _slots.DeleteAsync(id, ct)).ToActionResult(this);
}
