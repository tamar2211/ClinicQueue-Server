using AutoMapper;
using ClinicQueue.Application.Common.Results;
using ClinicQueue.Application.DTOs.Auth;
using ClinicQueue.Application.Interfaces;
using ClinicQueue.Application.Mapping;
using ClinicQueue.Application.Services;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using Moq;

namespace ClinicQueue.Tests;

public class AuthServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("HASHED");
        _tokens.Setup(t => t.CreateToken(It.IsAny<User>())).Returns(("token", DateTime.UtcNow.AddHours(1)));

        var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
        _sut = new AuthService(_users.Object, _unitOfWork.Object, _hasher.Object, _tokens.Object, mapper);
    }

    [Fact]
    public void MappingConfiguration_IsValid() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>()).AssertConfigurationIsValid();

    [Fact]
    public async Task Register_NewEmail_CreatesPatientWithHashedPasswordAndNormalizedEmail()
    {
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), Ct)).Callback<User, CancellationToken>((u, _) => saved = u);

        var result = await _sut.RegisterAsync(
            new UserRegisterRequest { FullName = "Dana", Email = "  Dana@Example.COM ", Phone = "050-1234567", Password = "secret123" }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("dana@example.com", saved!.Email);
        Assert.Equal(UserRole.Patient, saved.Role);
        Assert.Equal("HASHED", saved.PasswordHash);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflictAndSavesNothing()
    {
        _users.Setup(u => u.EmailExistsAsync("dana@example.com", Ct)).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(
            new UserRegisterRequest { FullName = "Dana", Email = "dana@example.com", Phone = "050-1234567", Password = "secret123" }, Ct);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _unitOfWork.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsTokenAndRole()
    {
        _users.Setup(u => u.GetByEmailAsync("admin@clinic.com", Ct))
            .ReturnsAsync(new User { Id = 1, Email = "admin@clinic.com", PasswordHash = "H", Role = UserRole.Admin });
        _hasher.Setup(h => h.Verify("pw", "H")).Returns(true);

        var result = await _sut.LoginAsync(new UserLoginRequest { Email = "Admin@Clinic.com", Password = "pw" }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.Token);
        Assert.Equal("Admin", result.Value.Role);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnUnauthorizedWithSameMessage()
    {
        _users.Setup(u => u.GetByEmailAsync("a@a.com", Ct)).ReturnsAsync(new User { PasswordHash = "H" });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "H")).Returns(false);

        var wrongPassword = await _sut.LoginAsync(new UserLoginRequest { Email = "a@a.com", Password = "x" }, Ct);
        var unknownEmail = await _sut.LoginAsync(new UserLoginRequest { Email = "none@a.com", Password = "x" }, Ct);

        Assert.Equal(ErrorType.Unauthorized, wrongPassword.ErrorType);
        Assert.Equal(ErrorType.Unauthorized, unknownEmail.ErrorType);
        Assert.Equal(wrongPassword.ErrorMessage, unknownEmail.ErrorMessage);
    }
}
