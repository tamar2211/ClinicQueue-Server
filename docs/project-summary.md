# ClinicQueue – סיכום הפרויקט ללימוד והגשה

מצב: השרת גמור ונבדק (עד commit `0b60a72`). נשאר: לקוח React, העלאה ל-Render, README.
המסמך מסביר מה נבנה, איפה זה בקוד, ולמה. בסוף יש שאלות שבודק עשוי לשאול ותשובות קצרות.

---

## 1. מה המערכת עושה
מערכת תורים למרפאה. מטופלת נרשמת, מחפשת משבצת זמן אצל רופא, וקובעת תור. מנהלת (מזכירה) מנהלת רופאים, משבצות ותורים.

**המשאב המוגבל: `DoctorSlot` (משבצת זמן אצל רופא).** רק מטופלת אחת יכולה לתפוס אותה.
המשפט שהמורה ביקשה: *"שתי מטופלות לחצו בו-זמנית על אותה משבצת, שתיהן היו מקבלות אישור, ואסור היה ששתיהן יקבלו"*. כל הפרויקט בנוי סביב מניעת המצב הזה.

---

## 2. ארכיטקטורה (Clean Architecture)

```
ClinicQueue.sln
  ClinicQueue.Domain          ישויות ו-enums בלבד. לא תלוי בכלום.
  ClinicQueue.Application     לוגיקה עסקית: Services, Interfaces, DTOs, Result, Mapping
  ClinicQueue.Infrastructure  DbContext, Repositories, Migrations, JWT, BCrypt, Seed
  ClinicQueue.API             Controllers, Middleware, Program.cs
  ClinicQueue.Tests           xUnit + Moq + SQLite
```

**כיוון התלויות:** `Domain ← Application ← Infrastructure`, ו-`API → Application + Infrastructure`.
- Domain לא מפנה לשום פרויקט ולא לחבילות EF Core (הטעות הנפוצה שהמורה מורידה עליה ניקוד).
- Application מגדיר Interfaces (`IUserRepository`, `ITokenService`, `IPasswordHasher`...) ו-Infrastructure מממש אותם. כך ה-Service לא יודע שיש EF, PostgreSQL או BCrypt.
- ה-API מכיר את Infrastructure **רק** כדי לרשום שירותים ב-`Program.cs`.
- Controller לא מזריק DbContext. הוא מדבר רק עם Service.

**למה זה טוב:** אפשר להחליף מסד נתונים או ספריית הצפנה בלי לגעת בלוגיקה, ואפשר לבדוק את ה-Service עם Mock.

**Dependency Injection** (הכול דרך constructor, הרישום ב-`Program.cs`):
- `AppDbContext`, Repositories, `UnitOfWork`, Services: **Scoped** (מופע אחד לכל בקשה).
- `IPasswordHasher`, `ITokenService`: **Singleton** (חסרי מצב).
- למה לא Singleton ל-DbContext: DbContext אינו thread-safe ושומר מצב של בקשה. Singleton שמחזיק Scoped "קופא" על DbContext של הבקשה הראשונה ושובר את כל הבקשות הבאות.

---

## 3. מודל הנתונים

| ישות | שדות עיקריים | הערות |
|---|---|---|
| `User` | FullName, Email (ייחודי), Phone, PasswordHash, Role, **Version** | Role: Patient/Admin. נשמר כטקסט |
| `Doctor` | FullName, IsActive | לא מוחקים רופא עם משבצות, משביתים |
| `Specialization` | Name (ייחודי) | |
| `DoctorSlot` | DoctorId, StartTimeUtc, EndTimeUtc, IsBooked, **Version** | **המשאב המוגבל** |
| `Appointment` | UserId, DoctorSlotId, Status, CreatedAtUtc, CancelledAtUtc | Status: Active / CancelledByPatient / CancelledByClinic |

**קשרים (מוגדרים ב-Fluent API ב-`AppDbContext`):**
- **one-to-many:** Doctor → Slots (`Restrict`), DoctorSlot → Appointments (`Cascade`), User → Appointments (`Restrict`).
- **many-to-many:** Doctor ↔ Specialization דרך טבלת קישור `DoctorSpecializations`.
- ביטול תור **לא מוחק**: משנה Status ונשאר בהיסטוריה (לכן משבצת אחת יכולה להכיל כמה תורים לאורך זמן, אבל רק אחד פעיל).
- כל הזמנים **UTC** (דרישה של PostgreSQL/Npgsql). ההמרה לשעון ישראל נעשית רק לצורך "יום שלם" בחיפוש, ובתצוגה בלקוח.

**אינדקסים:** Email ייחודי; (DoctorId, StartTimeUtc); (UserId, Status); ו**אינדקס ייחודי חלקי** על `Appointments.DoctorSlotId` רק כש-`Status = 'Active'` (ראו סעיף 6).

**Migrations (3):** `Init`, `UniqueActiveAppointmentPerSlot`, `AddUserVersion`.
**Seed:** התמחויות ורופאים ב-`HasData`; משתמשי דמו ומשבצות ב-`DbSeeder` (תלויים בתאריך ובהצפנת סיסמה). רץ בעליית השרת, **בטוח להרצה חוזרת** (מוסיף רק מה שחסר). בעליית השרת גם `MigrateAsync`, כך שאין צורך בפעולה ידנית במסד.

---

## 4. ה-API (כל הכתובות)

סטטוסים: 200 הצלחה · 201+Location יצירה · 204 בלי תוכן · 400 קלט/ולידציה · 401 לא מחוברת · 403 אין הרשאה · 404 לא נמצא · 409 התנגשות.

| Method + נתיב | הרשאה | תוצאות |
|---|---|---|
| `POST /api/auth/register` | פתוח | 200 (token), 400, 409 אימייל קיים |
| `POST /api/auth/login` | פתוח | 200 (token), 400, 401 |
| `GET /api/specializations` | מחוברת | 200 |
| `GET /api/doctors` · `GET /api/doctors/{id}` | מחוברת | 200, 404 |
| `POST /api/doctors` | **Admin** | 201+Location, 400 |
| `PUT /api/doctors/{id}` | **Admin** | 204, 400, 404, 409 (השבתה עם תורים עתידיים) |
| `DELETE /api/doctors/{id}` | **Admin** | 204, 404, 409 (יש משבצות) |
| `GET /api/slots?doctorId&specializationId&date&onlyAvailable&page&pageSize` | מחוברת | 200 עם pagination |
| `GET /api/slots/{id}` | מחוברת | 200, 404 |
| `POST /api/slots` | **Admin** | 201+Location, 400, 404, 409 (חפיפה) |
| `DELETE /api/slots/{id}` | **Admin** | 204, 404, 409 (תפוסה) |
| `POST /api/appointments` | **Patient** | 201+Location, 400, 404, **409** |
| `GET /api/appointments/{id}` | בעלת התור / Admin | 200, 403, 404 |
| `GET /api/appointments/mine` | מחוברת | 200 עם pagination |
| `GET /api/appointments` | **Admin** | 200 עם pagination |
| `DELETE /api/appointments/{id}` | בעלת התור / Admin | 204, 400, 403, 404, 409 |

הערות עיצוב:
- ביטול תור הוא `DELETE` שמשנה סטטוס (לא מוחק). REST: פעולה דרך ה-method ולא דרך שם בכתובת.
- הרשמה מחזירה 200 עם token (כמו התחברות), כי אין משאב שאפשר לשלוף ב-`Location`.
- Binding: `[FromRoute]` למזהים, `[FromQuery]` לסינון ו-pagination, `[FromBody]` לגוף.
- ולידציה: Data Annotations על ה-DTOs (צורה: חובה, אורך, פורמט). **ולידציה עסקית (שתלויה בנתונים) רק ב-Service.**
- זהות המטופלת נלקחת **רק מה-JWT**, לא מהגוף (אי אפשר לקבוע תור בשם מישהי אחרת).

**Pagination:** ברירת מחדל 10, מקסימום 50, `Skip/Take` בתוך השאילתה ב-DB, עם `CountAsync` לסך הכול. `AsNoTracking` בכל קריאה בלבד. הקרנה ישר ל-DTO (`Select`) ולכן בלי N+1; רשימת רופאים כוללת `AvailableSlotsCount` בשאילתה אחת.

---

## 5. מסלול של בקשה (Pipeline)

סדר ה-middleware ב-`Program.cs`, **שגיאות ראשון**:
1. `ExceptionHandlingMiddleware`: תופס כל חריגה ומחזיר JSON אחיד (500 הודעה כללית, 409 להתנגשות).
2. `CorrelationIdMiddleware`: מזהה ייחודי לכל בקשה, ב-header `X-Correlation-Id` ובכל שורת לוג.
3. Swagger (בפיתוח) → HTTPS redirect → **CORS** → **Authentication** → **Authorization** → Controllers.

**פורמט שגיאה אחיד לכל המערכת:** `{ "status": 409, "message": "...", "errors": { "שדה": ["הודעה"] } }` (`errors` רק בשגיאות ולידציה).

**Result pattern:** ה-Service לא זורק חריגה על כל מקרה צפוי. הוא מחזיר `Result<T>` עם `ErrorType` (Validation/Unauthorized/Forbidden/NotFound/Conflict), ו-`ResultExtensions` ב-API ממיר ל-400/401/403/404/409. חריגות שמורות למצבים לא צפויים.

---

## 6. לב הפרויקט: התחרות על המשבצת

**הגישה: Optimistic Concurrency** (כנדרש). לא נועלים כלום; מזהים התנגשות בזמן השמירה.

**שלוש שכבות הגנה:**
1. **`Version` (Guid) על `DoctorSlot`** מוגדר `IsConcurrencyToken()` ב-Fluent API. ה-`SaveChangesAsync` (דריסה ב-`AppDbContext`) מחליף Version בכל שינוי. EF שולח `UPDATE ... WHERE Id=@id AND Version=@ישן`. מי שהגיעה שנייה: אף שורה לא התעדכנה ← `DbUpdateConcurrencyException`.
2. **המרה ל-409:** `UnitOfWork` (Infrastructure) תופס **רק** `DbUpdateConcurrencyException` ומחליף ב-`ConcurrencyConflictException` (מוגדרת ב-Application). ה-Service תופס אותה, כותב **Warning** ללוג ומחזיר `Result.Conflict` ← 409 עם הודעה ברורה.
   למה חריגה משלנו: שה-Service לא יצטרך להכיר את EF (שמירה על הפרדת שכבות).
3. **אינדקס ייחודי חלקי ב-DB:** `UNIQUE (DoctorSlotId) WHERE Status='Active'`. גם אם משהו עוקף את ה-Version, ה-DB לא ייתן שני תורים פעילים על משבצת. `UnitOfWork` ממיר גם את הפרת האינדקס (Postgres `23505`) ל-409.

**קביעת תור (`AppointmentService.CreateAsync`), לפי הסדר:**
משבצת קיימת (404) ← לא תפוסה (409) ← לא בעבר (400) ← רופא פעיל (400) ← למטופלת אין תור פעיל חופף (409) ← מסמנים `IsBooked=true`, "נוגעים" ב-User, יוצרים Appointment ← **שמירה אחת** (טרנזקציה אחת). אם בינתיים מישהי שינתה את המשבצת, ה-Version לא תואם ← 409.
> הבדיקה והשמירה באותה טרנזקציה. בדיקה שרצה לפני הטרנזקציה "לא שווה כלום" (המורה).

**החור שסגרנו: אותה מטופלת, שתי בקשות מקבילות על משבצות חופפות של רופאים שונים.** כל משבצת מתעדכנת בנפרד ולכן ה-Version של כל אחת תקין. הפתרון: גם ל-`User` יש `Version`, וכל קביעת תור "נוגעת" בשורת המשתמשת (`Touch`) ← שתי הקביעות מתנגשות על שורת המשתמשת. אותה טכניקה optimistic.

**ביטול תור:** בעלת התור בלבד (או מנהלת), לא יותר מ-24 שעות לפני (מנהלת פטורה, מסומן `CancelledByClinic`), המשבצת חוזרת להיות פנויה. הביטול משנה את המשבצת ולכן מוגן גם הוא ב-Version.

**ההוכחה (`ConcurrencyTests`, חובה לפי המורה):** שני `DbContext` נפרדים טוענים את אותה משבצת, שניהם משנים, הראשון נשמר והשני מקבל `DbUpdateConcurrencyException`. כשמבטלים זמנית את ה-token הבדיקה נכשלת, כלומר היא אמיתית.

---

## 7. אבטחה והגדרות
- **הרשמה:** תמיד `Patient`. אימייל עובר `Trim` + אותיות קטנות (ב-PostgreSQL השוואה רגישה לאותיות).
- **סיסמאות:** BCrypt (work factor 12). לא נשמר טקסט גלוי. בהתחברות משווים עם `Verify` (אי אפשר "לפענח" hash).
- **התחברות:** אימייל שגוי וסיסמה שגויה מחזירים **אותה הודעה** (לא חושפים איזה מהם).
- **JWT:** claims: מזהה משתמש, אימייל, role. תוקף 60 דקות. נבדקים issuer, audience, תוקף, חתימה. `ClockSkew = 0`.
- **הרשאות בפועל:** `[Authorize]` על הכול חוץ מהרשמה/התחברות; `[Authorize(Roles="Admin")]` ליצירה/עדכון/מחיקה של רופאים ומשבצות ולרשימת כל התורים; `[Authorize(Roles="Patient")]` לקביעת תור.
- **סודות:** connection string ו-`Jwt:Key` ב-**User Secrets** (בפיתוח) ובמשתני סביבה (ב-Render). `appsettings.json` מכיל רק ערכים לא סודיים (Issuer, Audience, CORS). אין סוד ב-repo.
- **CORS:** רק כתובות הלקוח המוגדרות. `X-Correlation-Id` חשוף ללקוח.

---

## 8. לוגים (NLog)
- NLog מתחת ל-`ILogger` הרגיל. כותב לקובץ `logs/clinicqueue-<תאריך>.log` ולקונסול.
- פורמט: `זמן|רמה|CorrelationId|מחלקה|הודעה`.
- **Information:** כל בקשה נכנסת (method+path), סיום בקשה (קוד+זמן), תור נקבע/בוטל.
- **Warning:** התנגשות על משאב. **Error:** חריגה לא צפויה.
- **לא נכתבים** סיסמאות, tokens, גוף בקשה או query string.

---

## 9. בדיקות (31, כולן עוברות)
- `AppointmentServiceTests` (12, Moq): קביעה תקינה/נדחית (תפוסה, לא קיימת, בעבר, רופא לא פעיל, חפיפה, התנגשות+Warning), ביטול (בעלת/אחרת/פחות מ-24ש/מנהלת/כבר בוטל).
- `AuthServiceTests` (5): תפקיד Patient, hash, אימייל מנורמל, כפול, התחברות, הודעת שגיאה זהה, ותקינות תצורת AutoMapper.
- `DoctorServiceTests` + `SlotServiceTests` (9).
- `ConcurrencyTests` (5, SQLite בזיכרון): שני DbContext על משבצת, המרת UnitOfWork, אותה מטופלת, האינדקס החלקי.
- הערה: בדיקות ה-DbContext רצות על SQLite ולא PostgreSQL. מנגנון ה-Version הוא של EF עצמו, ולכן ההוכחה תקפה.

---

## 10. הרצה מקומית
1. Docker: `docker run --name pg -e POSTGRES_PASSWORD=<הסיסמה> -p 5432:5432 -d postgres:16`
2. `dotnet user-secrets init --project ClinicQueue.API` ואז הגדרת `ConnectionStrings:Default` ו-`Jwt:Key` (בחלון PowerShell נפרד).
3. `dotnet run --project ClinicQueue.API` (עושה Migrate + Seed לבד). Swagger ב-`http://localhost:5207/swagger`.
4. בדיקות: `dotnet test`.
5. משתמשי דמו: `admin@clinic.com` (Admin) ו-`patient@clinic.com` (Patient), סיסמה `Demo1234!`.

---

## 11. שאלות שהבודק עשוי לשאול

- **למה optimistic ולא pessimistic?** בלי נעילות והמתנות ובלי deadlock; ההתנגשות נדירה, והמחיר שמישהי תנסה שוב. המורה דרשה optimistic.
- **למה `Version` (Guid) ולא `xmin`/`rowversion`?** `rowversion` לא קיים ב-PostgreSQL. `Version` עובד על כל מסד, קל להבנה, ומתחלף במקום אחד (`SaveChangesAsync`).
- **מה קורה בדיוק כששתיים לוחצות יחד?** שתיהן קוראות משבצת פנויה; הראשונה שומרת ו-Version מתחלף; ה-UPDATE של השנייה לא מוצא שורה ← חריגה ← 409.
- **למה 409 ולא 400/500?** הבקשה תקינה בפני עצמה אבל מתנגשת עם מצב המשאב.
- **מה ההבדל בין הבדיקה לאינדקס החלקי?** ה-Version מגן על המצב הנוכחי; האינדקס רשת ביטחון של ה-DB שלא תלויה בקוד.
- **למה ה-Service לא תופס `DbUpdateConcurrencyException`?** היא שייכת ל-EF. ה-UnitOfWork ממיר אותה לחריגה של Application כדי לשמור על הפרדת השכבות.
- **למה Result pattern?** מקרים צפויים ("תפוס", "לא נמצא") לא צריכים חריגה; הקוד קריא ומתורגם ישר לסטטוס.
- **מה זה N+1 ואיך נמנע?** שאילתה לכל פריט ברשימה. אצלנו הקרנה (`Select`) ו-`Include` בשאילתה אחת.
- **למה DTOs?** ישויות לא יוצאות ולא נכנסות דרך ה-API (אבטחה: אין mass assignment, ואין חשיפת PasswordHash).
- **למה זמנים ב-UTC?** Npgsql ממפה ל-`timestamptz` וזורק על שעה מקומית; ממירים רק בתצוגה.
- **איפה הולידציה העסקית?** ב-Service (תלויה בנתונים). Data Annotations רק לצורה.
- **למה אין DELETE שמוחק תור?** ביטול = שינוי סטטוס, כדי לשמור היסטוריה.

---

## 12. מיפוי לרשימת המורה (חלק ב)
1. REST וסטטוסים, Swagger: Controllers + `ProducesResponseType`.
2. Binding וולידציה: DTOs ב-`Application/DTOs`, ה-Services.
3. שכבות: ארבעה פרויקטים + Tests (סעיף 2).
4. DI: `Program.cs`.
5. EF Code-First, 2+ migrations, Seed, connection string מקונפיגורציה: `Infrastructure/Migrations`, `DbSeeder`.
6. CRUD, סינון, מיון, pagination, AsNoTracking: Repositories.
7. one-to-many ו-many-to-many, Include, Fluent API: `AppDbContext`, `DoctorRepository`.
8. DTO ו-AutoMapper: `Mapping/MappingProfile`.
9. async + CancellationToken: בכל השרשרת.
10. Middleware (שגיאות, CorrelationId), סדר נכון: `API/Middleware`, `Program.cs`.
11. Configuration, User Secrets: `appsettings*.json`, `JwtSettings` (IOptions).
12. JWT, 2 roles, `[Authorize]`: `AuthService`, `JwtTokenService`.
13. xUnit+Moq על Services והמשאב המוגבל: `ClinicQueue.Tests`.
- חלק ג (תחרות): סעיף 6. חלק ד (NLog): סעיף 8.

---

## 13. נקודות חולשה והסתייגויות (טוב להכיר מראש)
- **AutoMapper 13.0.1** עם אזהרת פגיעות ידועה (DoS, GHSA-rvv3-g6hj-g44x, תוקנה בגרסה 15.1.1+, שדורשת license key). אנחנו ממפים רק DTOs שטוחים. החלטה מודעת, יש לציין ב-README.
- בדיקות ה-DbContext על SQLite (לא PostgreSQL).
- שתי יצירות משבצת חופפות במקביל על ידי מנהלות עשויות לעבור את בדיקת החפיפה (פעולת ניהול נדירה, לא טופל).
- בדיקת הלוג של Warning ההתנגשות נעשית ב-Mock; בבדיקות HTTP מקבילות 409 הגיעו מהבדיקות המקדימות ולא מה-Version, ולכן ההוכחה למנגנון היא בבדיקות ה-DbContext.

## 14. מה נשאר
לקוח React (6 מסכים), העלאה ל-Render (שרת + לקוח + PostgreSQL), שני repositories ב-GitHub, README, ובדיקה אחרונה מול הרשימה.
