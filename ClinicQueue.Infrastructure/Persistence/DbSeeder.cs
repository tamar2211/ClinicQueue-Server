using ClinicQueue.Application.Interfaces;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Persistence;

/// <summary>
/// נתוני דמו שתלויים בתאריך ובהצפנת סיסמאות. בטוח להרצה חוזרת: מוסיף רק מה שחסר.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _hasher;

    public DbSeeder(AppDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        await SeedUserAsync("מנהלת המרפאה", "admin@clinic.com", "050-0000001", UserRole.Admin, ct);
        await SeedUserAsync("מטופלת לדוגמה", "patient@clinic.com", "050-0000002", UserRole.Patient, ct);
        await SeedSlotsAsync(ct);
        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedUserAsync(string name, string email, string phone, UserRole role, CancellationToken ct)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email, ct)) return;

        _context.Users.Add(new User
        {
            FullName = name,
            Email = email,
            Phone = phone,
            Role = role,
            PasswordHash = _hasher.Hash("Demo1234!")
        });
    }

    // רק אם אין עדיין משבצות: ימי חול הקרובים, חצי שעה כל אחת (UTC)
    private async Task SeedSlotsAsync(CancellationToken ct)
    {
        if (await _context.DoctorSlots.AnyAsync(ct)) return;

        var doctorIds = await _context.Doctors.Select(d => d.Id).ToListAsync(ct);
        var firstDay = DateTime.UtcNow.Date.AddDays(1);

        for (var day = 0; day < 7; day++)
        {
            var date = firstDay.AddDays(day);
            if (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) continue;

            foreach (var doctorId in doctorIds)
                for (var half = 0; half < 6; half++)
                {
                    var start = date.AddHours(7).AddMinutes(30 * half);
                    _context.DoctorSlots.Add(new DoctorSlot
                    {
                        DoctorId = doctorId,
                        StartTimeUtc = start,
                        EndTimeUtc = start.AddMinutes(30)
                    });
                }
        }
    }
}
