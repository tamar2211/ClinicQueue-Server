using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await _context.Users.AddAsync(user, ct);
}
