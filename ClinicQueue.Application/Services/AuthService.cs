using AutoMapper;
using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Auth;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;

namespace ClinicQueue.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IMapper _mapper;

    public AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher,
        ITokenService tokens, IMapper mapper)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _mapper = mapper;
    }

    public async Task<Result<UserLoginResponse>> RegisterAsync(UserRegisterRequest request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        if (await _users.EmailExistsAsync(email, ct))
            return Result<UserLoginResponse>.Conflict("האימייל כבר רשום במערכת");

        var user = _mapper.Map<User>(request);
        user.Email = email;
        user.Role = UserRole.Patient;
        user.PasswordHash = _hasher.Hash(request.Password);

        await _users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserLoginResponse>.Success(ToResponse(user));
    }

    public async Task<Result<UserLoginResponse>> LoginAsync(UserLoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(Normalize(request.Email), ct);

        // אותה הודעה לאימייל שגוי ולסיסמה שגויה - לא חושפים מה מהם לא נכון
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return Result<UserLoginResponse>.Unauthorized("אימייל או סיסמה שגויים");

        return Result<UserLoginResponse>.Success(ToResponse(user));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private UserLoginResponse ToResponse(User user)
    {
        var (token, expiresAtUtc) = _tokens.CreateToken(user);
        return new UserLoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
