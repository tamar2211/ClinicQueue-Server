using ClinicQueue.Application.Common.Exceptions;
using ClinicQueue.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClinicQueue.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private const string ConflictMessage = "הפעולה התנגשה עם פעולה אחרת שהתבצעה באותו זמן. נסי שוב.";

    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        // חייב להיות לפני DbUpdateException: DbUpdateConcurrencyException יורשת ממנה
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ConflictMessage, ex);
        }
        // הפרת אינדקס ייחודי (למשל תור פעיל שני על אותה משבצת): הרשת השנייה של ה-DB
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConcurrencyConflictException(ConflictMessage, ex);
        }
    }
}
