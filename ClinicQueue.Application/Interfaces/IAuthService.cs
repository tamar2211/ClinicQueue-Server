using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Auth;

namespace ClinicQueue.Application.Interfaces;

public interface IAuthService
{
    Task<Result<UserLoginResponse>> RegisterAsync(UserRegisterRequest request, CancellationToken ct);
    Task<Result<UserLoginResponse>> LoginAsync(UserLoginRequest request, CancellationToken ct);
}
