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

## מצב נוכחי (16.10.2026) – כ-75% מהפרויקט (השרת)
**השרת גמור ונבדק** (commit אחרון בקוד: 0b60a72; אחריו נוספו docs/project-summary.md ו-.html). Build נקי, 31 בדיקות עוברות.
- מבנה: Domain ← Application ← Infrastructure, API → Application + Infrastructure, Tests. הכול מקומי, **עוד אין push** ואין repository ב-GitHub.
- בשרת: Auth (JWT+BCrypt), Doctors (CRUD, DELETE רק בלי משבצות), Specializations, Slots, Appointments, middleware (שגיאות+CorrelationId),
  NLog, CORS (Cors:AllowedOrigins, ברירת מחדל http://localhost:5173), 3 migrations, DbSeeder, MigrateAsync בעליית השרת.
- תחרות: Version על DoctorSlot **ועל User** (סוגר את החור של אותה מטופלת), אינדקס חלקי לתור פעיל אחד למשבצת, UnitOfWork ממיר גם 23505. ביטול ב-DELETE (משנה סטטוס), חלון 24 שעות (מנהלת פטורה).
- הסבר מלא ללימוד ולבודק: docs/project-summary.md (+ .html). חומר ל-README: docs/concurrency-notes.md.
- DB: Docker, קונטיינר pg (`docker start pg` אם כבוי), DB ClinicQueueDb. connection string ו-Jwt:Key ב-User Secrets (**הסיסמה לא מוכרת ל-Claude**, לא להדפיס סודות).
  לפקודות עם סיסמה: חלון PowerShell של הסטודנטית, לא `!`. הדבקת עברית בטרמינל מתהפכת, לכן לא `cd` עם נתיב בעברית (`cd D:\Users\User\Desktop\*\.NetProject\ClinicQueue`).
- דמו: admin@clinic.com / patient@clinic.com, סיסמה Demo1234!. dotnet-ef מותקן גלובלית. הרצה: `dotnet run --project ClinicQueue.API` (http://localhost:5207/swagger).
- AutoMapper 13.0.1: אזהרת פגיעות ידועה (תוקן ב-15.1.1+ שדורש license key). הוחלט להישאר ולהזכיר ב-README.
- בדיקות ה-DbContext על SQLite בזיכרון (לא Postgres). לציין ב-README.
- לוגים: Claude מחליט לבד (זיכרון logging-autonomy). שאר הדברים: עדיין צעד-צעד עם אישור, commit רק באישור.

## השלבים הבאים (בסדר הזה)
1. **לקוח React, 6 מסכים (אושר):** התחברות/הרשמה (כפתורי כניסה מהירה לדמו), רשימת משבצות עם סינון ו-pagination, קביעת תור,
   הודעת 409 ברורה, "התורים שלי" (עם ביטול), מצב מנהלת (כל התורים, יצירת משבצת, ניהול רופאים). עיצוב נקי ומודרני, RTL.
   הלקוח לא נבדק בציון, אבל הבודק משתמש בו לבדוק את המערכת. Claude בונה לבד (זיכרון client-not-graded). repository נפרד.
2. העלאה ל-Render (שרת + לקוח + PostgreSQL בענן, משתני סביבה כולל Cors__AllowedOrigins__0) ושני repositories ב-GitHub. לפני push: לוודא שאין סודות בהיסטוריה. push רק באישור.
3. README אחרון (פסקה, המשאב המוגבל, הרצה מקומית עם Docker, משתמשי דמו, כתובות חיות) ובדיקה אחרונה מול docs/teacher-requirements.md.

## שאלות פתוחות
אין. נסגרו: חלון ביטול 24 ש' · PostgreSQL ב-Docker · שני תורים חופפים למטופלת – 409 · כיתה: סמינר · 6 מסכים.
