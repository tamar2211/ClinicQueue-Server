# ClinicQueue – פרויקט סיום .NET Web API

## בתחילת כל שיחה
1. קרא את הקובץ הזה עד הסוף.
2. קרא את `docs/teacher-requirements.md` – ההנחיות המלאות של המורה. כל קוד חייב לעמוד בהן.
3. ספר לסטודנטית בשורה אחת באיזה שלב אנחנו ומה הצעד הבא, ושאל אם להמשיך.

## אופן עבודה (חובה)
- הסטודנטית מחליטה. לפני כל החלטה: רקע קצר + המלצה, ומחכים לאישור.
- עדכון קצר, פשוט וברור על כל צעד, גם הקטן ביותר. לא מתקדמים בלי אישור.
- לוודא שהיא מבינה מה אישרה.
- קוד קצר ונקי, בלי שורות מיותרות.
- לפני כל מחיקה: commit ל-Git.
- אחרי כל שינוי קוד: `dotnet build`, ולדווח על התוצאה.
- תשובות בעברית.

## הרעיון
מערכת תורים למרפאה. המשאב המוגבל: DoctorSlot (משבצת זמן אצל רופא), שרק מטופלת אחת יכולה לתפוס.
Optimistic concurrency: שדה Guid Version, מתחלף בכל שמירה ב-SaveChangesAsync.

## דרישות המורה (תקציר – הפירוט ב-docs/teacher-requirements.md)
REST וסטטוסים נכונים (200/201+Location/204/400/401/403/404/409), Swagger · FromRoute/FromQuery/FromBody,
Data Annotations על DTOs, ולידציה עסקית ב-Service · DI דרך constructor · EF Core Code-First, PostgreSQL,
2+ migrations, connection string מהקונפיגורציה, Seed · CRUD, סינון, מיון, pagination עם Skip/Take בתוך DB,
AsNoTracking · one-to-many ו-many-to-many, Include, Fluent API, בלי N+1 · DTOs ו-AutoMapper ·
async עד הסוף עם CancellationToken · Middleware: שגיאות גלובלי (JSON אחיד) ו-CorrelationId ·
appsettings.Development, User Secrets, אין סודות ב-repo · JWT, 2 roles, [Authorize] ·
xUnit ו-Moq על Services, כולל המשאב המוגבל (תקין ונדחה) · concurrency: catch ספציפי ל-DbUpdateConcurrencyException
והחזרת 409, ובדיקה עם שני DbContext · NLog: בקשה עם CorrelationId (Info), חריגה (Error), התנגשות (Warning),
בלי סיסמאות או tokens · README · לקוח React · העלאה ל-Render.

## החלטות
- ‎.NET 8. תיקייה: D:\Users\User\Desktop\שנה ב\.NetProject\ClinicQueue
- Roles: Patient, Admin (מזכירה = מנהל). הרשמה = תמיד Patient.
- ישויות: User, Doctor, Specialization (M:N עם Doctor), DoctorSlot, Appointment (Slot→Appointments 1:N, היסטוריית ביטולים).
- ביטול = שינוי Status ולא מחיקה. זמנים ב-UTC.
- משבצת: אורך חופשי, יצירה אחת-אחת. חיפוש: רופא, התמחות, תאריך, רק פנויות.
- Pagination: 10 כברירת מחדל, מקסימום 50. רופאים בלי pagination.
- DoctorDetailsResponse כולל AvailableSlotsCount (שאילתה אחת).
- Result pattern + ErrorType. ConcurrencyConflictException: Infrastructure ממיר אליה את DbUpdateConcurrencyException.
- שמות DTOs: קוראים כמו משפט. הישות עושה ← ישות קודם (UserRegisterRequest).
  עושים לישות ← פעולה קודם (CreateDoctorRequest). תצוגה ← XDetailsResponse.

## מצב נוכחי (16.10.2026) – כ-35% מהפרויקט
- המעבר ל-Clean Architecture הושלם ו-commit בוצע (מקומי, בלי push; main מקדים את origin ב-2). הפרויקטים הישנים נמחקו.
  מבנה: Domain ← Application ← Infrastructure, API → Application + Infrastructure, Tests. Build נקי.
- שמות DTOs הושלמו: AppointmentDetailsResponse, PagedResponse (כל השאר נשארו).
- Domain: Entities, Enums (בלי [ConcurrencyCheck]). Infrastructure: Persistence/AppDbContext (token ב-Fluent API).
- הושלמו (commit): UnitOfWork, 5 Repositories, 2 migrations, DbSeeder, BCrypt (IPasswordHasher), DI ו-MigrateAsync+Seed ב-Program.cs.
- DB: Docker, קונטיינר pg, ClinicQueueDb. connection string ב-User Secrets (הסיסמה לא מוכרת ל-Claude).
- דמו: admin@clinic.com / patient@clinic.com, סיסמה Demo1234!
- "לב הפרויקט" להסבר וב-README: docs/concurrency-notes.md.
- פתוח: UnitOfWork יחליף גם DbUpdateException של האינדקס החלקי ל-409.
- השלב הבא (לא אושר): Services + AutoMapper (גרסה בהחלטת הסטודנטית), מתחילים ב-AuthService (JWT).
- אחר כך: Controllers, Middleware, NLog, בדיקות, README.
- לקוח (לא נבדק): אחרי סיום השרת, עוברים יחד על המסכים כמפתח ומעצב, ואז Claude בונה לבד, בעיצוב מרשים. 4 מסכים חובה.
- חור לטיפול ב-AppointmentService: שתי בקשות מקבילות של אותה מטופלת על משבצות חופפות עוקפות את ה-Version של המשבצת.
- ב-Service: מטופלת מבטלת רק תור שלה (403), מנהל מבטל כל תור.

## שאלות פתוחות
כמה שעות לפני תור מותר לבטל (הצעה: 24).
נסגרו: PostgreSQL ב-Docker · שני תורים חופפים למטופלת – אסור (409). כיתה: סמינר.
