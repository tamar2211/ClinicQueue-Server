using ClinicQueue.Domain.Entities;

namespace ClinicQueue.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    /// מסמן את המשתמשת כשונתה (בלי לשנות שדה), כדי שה-Version שלה יתחלף בשמירה
    void Touch(User user);
    Task AddAsync(User user, CancellationToken ct);
}
