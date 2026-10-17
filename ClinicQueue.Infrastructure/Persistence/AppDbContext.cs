using ClinicQueue.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Infrastructure.Persistence;

/// <summary>
/// החיבור לבסיס הנתונים. כאן מוגדרות הטבלאות, הקשרים ביניהן (Fluent API)
/// ומנגנון ה-concurrency של המשבצות.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<DoctorSlot> DoctorSlots => Set<DoctorSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(150);
            e.Property(u => u.Phone).IsRequired().HasMaxLength(20);
            e.Property(u => u.PasswordHash).IsRequired();

            // נשמר כטקסט ("Patient"/"Admin") ולא כמספר - קריא יותר בבסיס הנתונים
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            // אימייל ייחודי - אי אפשר להירשם פעמיים עם אותו אימייל
            e.HasIndex(u => u.Email).IsUnique();

            e.Property(u => u.Version).IsConcurrencyToken();
        });

        // ---------- Specialization ----------
        modelBuilder.Entity<Specialization>(e =>
        {
            e.Property(s => s.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(s => s.Name).IsUnique();
        });

        // ---------- Doctor + קשר many-to-many עם Specialization ----------
        modelBuilder.Entity<Doctor>(e =>
        {
            e.Property(d => d.FullName).IsRequired().HasMaxLength(100);

            // טבלת קישור DoctorSpecializations (DoctorId, SpecializationId)
            e.HasMany(d => d.Specializations)
             .WithMany(s => s.Doctors)
             .UsingEntity<Dictionary<string, object>>(
                 "DoctorSpecializations",
                 r => r.HasOne<Specialization>().WithMany().HasForeignKey("SpecializationId"),
                 l => l.HasOne<Doctor>().WithMany().HasForeignKey("DoctorId"),
                 j =>
                 {
                     j.HasKey("DoctorId", "SpecializationId");
                     // נתוני דמו לטבלת הקישור
                     j.HasData(
                         new { DoctorId = 1, SpecializationId = 1 },
                         new { DoctorId = 1, SpecializationId = 2 },
                         new { DoctorId = 2, SpecializationId = 2 },
                         new { DoctorId = 3, SpecializationId = 3 },
                         new { DoctorId = 4, SpecializationId = 4 },
                         new { DoctorId = 5, SpecializationId = 5 },
                         new { DoctorId = 6, SpecializationId = 6 },
                         new { DoctorId = 7, SpecializationId = 7 },
                         new { DoctorId = 8, SpecializationId = 8 });
                 });
        });

        // ---------- DoctorSlot (המשאב המוגבל) ----------
        modelBuilder.Entity<DoctorSlot>(e =>
        {
            // one-to-many: רופא -> משבצות.
            // Restrict: אי אפשר למחוק רופא שיש לו משבצות (משביתים אותו עם IsActive במקום)
            e.HasOne(s => s.Doctor)
             .WithMany(d => d.Slots)
             .HasForeignKey(s => s.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            // ה-concurrency token - מוגדר רק כאן, לא על הישות ב-Domain
            e.Property(s => s.Version).IsConcurrencyToken();

            // אינדקס לחיפוש מהיר של משבצות לפי רופא ותאריך
            e.HasIndex(s => new { s.DoctorId, s.StartTimeUtc });
        });

        // ---------- Appointment ----------
        modelBuilder.Entity<Appointment>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);

            // one-to-many: משבצת -> היסטוריית תורים.
            // Cascade: מחיקת משבצת (מותרת רק כשהיא פנויה) מוחקת איתה את התורים המבוטלים שלה
            e.HasOne(a => a.DoctorSlot)
             .WithMany(s => s.Appointments)
             .HasForeignKey(a => a.DoctorSlotId)
             .OnDelete(DeleteBehavior.Cascade);

            // one-to-many: משתמש -> תורים. Restrict: לא מוחקים משתמש שיש לו תורים
            e.HasOne(a => a.User)
             .WithMany(u => u.Appointments)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.UserId, a.Status });

            // רשת ביטחון שנייה לתחרות: לכל היותר תור פעיל אחד למשבצת, גם אם ה-Version נעקף
            e.HasIndex(a => a.DoctorSlotId)
             .IsUnique()
             .HasFilter("\"Status\" = 'Active'");
        });

        // ---------- Seed: נתונים קבועים ----------
        // משתמשי דמו ומשבצות עתידיות ייווצרו בעליית השרת (DbSeeder) -
        // כי הם תלויים בתאריך של היום ובהצפנת סיסמאות.
        modelBuilder.Entity<Specialization>().HasData(
            new Specialization { Id = 1, Name = "רפואת משפחה" },
            new Specialization { Id = 2, Name = "רפואת ילדים" },
            new Specialization { Id = 3, Name = "רפואת עיניים" },
            new Specialization { Id = 4, Name = "רפואת עור" },
            new Specialization { Id = 5, Name = "אורתופדיה" },
            new Specialization { Id = 6, Name = "קרדיולוגיה" },
            new Specialization { Id = 7, Name = "אף אוזן גרון" },
            new Specialization { Id = 8, Name = "רפואת שיניים" });

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor { Id = 1, FullName = "ד\"ר משה כהן", IsActive = true },
            new Doctor { Id = 2, FullName = "ד\"ר יעקב לוי", IsActive = true },
            new Doctor { Id = 3, FullName = "ד\"ר אברהם פרידמן", IsActive = true },
            new Doctor { Id = 4, FullName = "ד\"ר רונית שמש", IsActive = true },
            new Doctor { Id = 5, FullName = "ד\"ר אורי בן דוד", IsActive = true },
            new Doctor { Id = 6, FullName = "ד\"ר נועה גולן", IsActive = true },
            new Doctor { Id = 7, FullName = "ד\"ר איתן מזרחי", IsActive = true },
            new Doctor { Id = 8, FullName = "ד\"ר תמר ברק", IsActive = true });
    }

    /// <summary>
    /// לפני כל שמירה: כל משבצת או משתמשת שהשתנו מקבלות Version חדש.
    /// EF שולח את ה-Version הישן בתנאי ה-WHERE ואת החדש ב-SET,
    /// כך שעדכון שמבוסס על נתון ישן לא ימצא שורה - ו-EF יזרוק DbUpdateConcurrencyException.
    /// הדריסה כאן חוסכת את הצורך לזכור לעשות את זה בכל Service.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RenewVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        RenewVersions();
        return base.SaveChanges();
    }

    private void RenewVersions()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
        {
            if (entry.Entity is DoctorSlot slot) slot.Version = Guid.NewGuid();
            else if (entry.Entity is User user) user.Version = Guid.NewGuid();
        }
    }
}
