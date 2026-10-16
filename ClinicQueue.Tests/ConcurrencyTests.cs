using ClinicQueue.Application.Common.Exceptions;
using ClinicQueue.Domain.Entities;
using ClinicQueue.Domain.Enums;
using ClinicQueue.Infrastructure.Persistence;
using ClinicQueue.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Tests;

/// <summary>
/// ההוכחה לתחרות על המשאב: שני DbContext נפרדים (שתי בקשות) קוראים את אותה שורה,
/// שניהם משנים, הראשון נשמר והשני נדחה. רץ על SQLite בזיכרון - מנגנון ה-Version
/// הוא של EF עצמו (Version הישן בתנאי ה-WHERE), ולכן לא תלוי ב-PostgreSQL.
/// </summary>
public sealed class ConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ConcurrencyTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();

        db.Users.AddRange(NewUser("a@test.com"), NewUser("b@test.com"));
        db.DoctorSlots.AddRange(NewSlot(hour: 8), NewSlot(hour: 8)); // שתי משבצות חופפות (אותה שעה)
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private static User NewUser(string email) =>
        new() { FullName = "Test", Email = email, Phone = "050-0000000", PasswordHash = "x" };

    private static DoctorSlot NewSlot(int hour) => new()
    {
        DoctorId = 1, // רופא מנתוני ה-Seed של ה-DbContext
        StartTimeUtc = new DateTime(2030, 1, 1, hour, 0, 0, DateTimeKind.Utc),
        EndTimeUtc = new DateTime(2030, 1, 1, hour, 30, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task TwoContexts_BookSameSlot_FirstSucceedsSecondGetsConcurrencyException()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        var slotInFirst = await first.DoctorSlots.FirstAsync();
        var slotInSecond = await second.DoctorSlots.FirstAsync();
        var versionBefore = slotInFirst.Version;

        slotInFirst.IsBooked = true;
        slotInSecond.IsBooked = true;

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotEqual(versionBefore, slotInFirst.Version); // ה-Version התחלף בשמירה
        await using var verify = NewContext();
        Assert.True((await verify.DoctorSlots.FirstAsync()).IsBooked);
    }

    [Fact]
    public async Task UnitOfWork_ConvertsConcurrencyExceptionToApplicationException()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        (await first.DoctorSlots.FirstAsync()).IsBooked = true;
        (await second.DoctorSlots.FirstAsync()).IsBooked = true;

        await new UnitOfWork(first).SaveChangesAsync(default);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(second).SaveChangesAsync(default));
    }

    [Fact]
    public async Task SamePatient_BooksTwoDifferentSlotsAtOnce_SecondGetsConcurrencyException()
    {
        // אין התנגשות על המשבצות (כל בקשה על משבצת אחרת), אבל שתיהן "נוגעות" באותה מטופלת
        await using var first = NewContext();
        await using var second = NewContext();
        var slots = await first.DoctorSlots.OrderBy(s => s.Id).ToListAsync();
        var slotForSecond = await second.DoctorSlots.OrderBy(s => s.Id).Skip(1).FirstAsync();

        var userInFirst = await first.Users.FirstAsync(u => u.Email == "a@test.com");
        var userInSecond = await second.Users.FirstAsync(u => u.Email == "a@test.com");

        slots[0].IsBooked = true;
        new UserRepository(first).Touch(userInFirst);
        slotForSecond.IsBooked = true;
        new UserRepository(second).Touch(userInSecond);

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task TwoActiveAppointmentsOnSameSlot_AreRejectedByUniqueIndex()
    {
        await using var db = NewContext();
        db.Appointments.Add(new Appointment { UserId = 1, DoctorSlotId = 1, Status = AppointmentStatus.Active });
        db.Appointments.Add(new Appointment { UserId = 2, DoctorSlotId = 1, Status = AppointmentStatus.Active });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotBlockNewActiveOneOnSameSlot()
    {
        await using var db = NewContext();
        db.Appointments.Add(new Appointment { UserId = 1, DoctorSlotId = 1, Status = AppointmentStatus.CancelledByPatient });
        db.Appointments.Add(new Appointment { UserId = 2, DoctorSlotId = 1, Status = AppointmentStatus.Active });

        await db.SaveChangesAsync(); // לא זורק: האינדקס חל רק על תורים פעילים
    }
}
