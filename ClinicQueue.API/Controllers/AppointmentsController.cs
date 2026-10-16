using System.Security.Claims;
using ClinicQueue.API.Extensions;
using ClinicQueue.Application.DTOs.Appointments;
using ClinicQueue.Application.DTOs.Common;
using ClinicQueue.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    // זהות המשתמשת נלקחת רק מה-token, אף פעם לא מגוף הבקשה
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.CreateAsync(CurrentUserId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById([FromRoute] int id, CancellationToken ct) =>
        (await _appointments.GetByIdAsync(id, CurrentUserId, IsAdmin, ct)).ToActionResult(this);

    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResponse<AppointmentDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetMine([FromQuery] PaginationQuery query, CancellationToken ct) =>
        (await _appointments.GetMineAsync(CurrentUserId, query, ct)).ToActionResult(this);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResponse<AppointmentDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll([FromQuery] PaginationQuery query, CancellationToken ct) =>
        (await _appointments.GetAllAsync(query, ct)).ToActionResult(this);

    /// ביטול תור: התור לא נמחק, הסטטוס שלו משתנה ונשאר בהיסטוריה
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancel([FromRoute] int id, CancellationToken ct) =>
        (await _appointments.CancelAsync(id, CurrentUserId, IsAdmin, ct)).ToActionResult(this);
}
