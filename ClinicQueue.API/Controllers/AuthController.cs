using ClinicQueue.API.Extensions;
using ClinicQueue.Application.DTOs.Auth;
using ClinicQueue.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register([FromBody] UserRegisterRequest request, CancellationToken ct) =>
        (await _auth.RegisterAsync(request, ct)).ToActionResult(this);

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] UserLoginRequest request, CancellationToken ct) =>
        (await _auth.LoginAsync(request, ct)).ToActionResult(this);
}
