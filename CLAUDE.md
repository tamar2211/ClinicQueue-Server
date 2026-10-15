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

## מצב נוכחי
- מבנה ישן: Core, Data, Service, API, Tests. הקוד משלבים 2-6 עוד לא התקמפל.
- אושר מעבר ל-Clean Architecture: Domain ← Application ← Infrastructure, ו-API → Application + Infrastructure.
  - Domain: Entities, Enums. בלי [ConcurrencyCheck], ההגדרה רק ב-Fluent API.
  - Application: DTOs לפי נושא, Common/Results, Common/Exceptions, Interfaces, Services.
  - Infrastructure: Persistence/AppDbContext, Repositories.
- השלב הבא: Build ← Git ו-commit ← מעבר ← Build ← commit.
- אחרי המעבר: לסיים שמות DTOs (SlotQueryParameters, Appointments, Common).

## שאלות פתוחות
כמה שעות לפני תור מותר לבטל (הצעה: 24) · לחסום שני תורים באותה שעה למטופלת (הצעה: כן, 409) ·
PostgreSQL דרך Docker או התקנה רגילה · כיתה: סמינר או שופרא.
