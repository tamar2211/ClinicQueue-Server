using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
