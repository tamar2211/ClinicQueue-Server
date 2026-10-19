# ClinicQueue – מערכת תורים למרפאה

שרת ASP.NET Core Web API לקביעת תורים במרפאה. מטופלים מחפשים תור פנוי לפי רופא, התמחות ותאריך, קובעים ומבטלים תורים. המזכירה (Admin) מנהלת רופאים ותורים פנויים ורואה את כל התורים במרפאה. הלב של המערכת הוא טיפול נכון בתחרות: תור פנוי אחד יכול להיתפס על ידי מטופל אחד בלבד, גם כשכמה מטופלים לוחצים באותו רגע.

| | |
|---|---|
| **שרת (Render)** | _יתווסף אחרי ההעלאה_ |
| **Swagger** | _יתווסף אחרי ההעלאה_ `/swagger` |
| **לקוח React** | _יתווסף אחרי ההעלאה_ |

**טכנולוגיות:** ‎.NET 8 · EF Core 8 · PostgreSQL · JWT · AutoMapper · NLog · xUnit + Moq

---

## המשאב המוגבל ואיך המערכת מגינה עליו

**המשאב:** `DoctorSlot`, תור פנוי אצל רופא.
**התחרות:** שני מטופלים שולחים `POST /api/appointments` על אותו תור באותו רגע. שתי הבקשות קוראות שהתור פנוי, ושתיהן היו נשמרות.

**הפתרון: Optimistic Concurrency**
- לכל `DoctorSlot` יש שדה `Guid Version`, שמוגדר כ-concurrency token ב-Fluent API.
- הדריסה של `SaveChangesAsync` ב-`AppDbContext` מחליפה את ה-Version בכל עדכון. EF שולח את הערך הישן בתנאי ה-`WHERE`, כך שהבקשה השנייה לא מוצאת שורה לעדכן ומקבלת `DbUpdateConcurrencyException`.
- `UnitOfWork` תופס את החריגה הספציפית הזו וממיר אותה לחריגה של שכבת Application. ה-Service מחזיר **409 Conflict** עם הודעה ברורה, ו-NLog רושם **Warning**.
- **רשת ביטחון ב-DB:** אינדקס ייחודי חלקי, `UNIQUE (DoctorSlotId) WHERE Status = 'Active'`, שלא מאפשר שני תורים פעילים על תור פנוי אחד.
- **אותו מטופל, שתי בקשות במקביל:** גם ל-`User` יש `Version`. לכן שתי קביעות מקבילות של אותו מטופל, גם על תורים שונים בשעה חופפת, מתנגשות זו בזו, והשנייה מקבלת 409.

**ההוכחה:** ב-`ClinicQueue.Tests/ConcurrencyTests.cs` שני `DbContext` נפרדים קוראים את אותו תור, ושניהם מנסים לתפוס אותו. הראשון נשמר, והשני מקבל `DbUpdateConcurrencyException`.

---

## הרצה מקומית

**דרישות:** ‎.NET 8 SDK ו-Docker Desktop.

**1. PostgreSQL ב-Docker:**
```bash
docker run --name pg -e POSTGRES_PASSWORD=<password> -p 5432:5432 -d postgres:16
```

**2. סודות (User Secrets), לא נשמרים ב-repository:**
```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=ClinicQueueDb;Username=postgres;Password=<password>" --project ClinicQueue.API
dotnet user-secrets set "Jwt:Key" "<מפתח אקראי באורך 32 תווים לפחות>" --project ClinicQueue.API
```

**3. הרצה:**
```bash
dotnet run --project ClinicQueue.API
```
בעליית השרת ה-migrations מוחלות ונתוני הדמו נטענים אוטומטית, בלי שום פעולה ידנית בבסיס הנתונים.
Swagger: http://localhost:5207/swagger

**בדיקות:**
```bash
dotnet test
```

---

## משתמשי דמו

| תפקיד | אימייל | סיסמה |
|---|---|---|
| Admin (מזכירה) | `admin@clinic.com` | `Demo1234!` |
| Patient (מטופל) | `patient@clinic.com` | `Demo1234!` |

ב-Swagger: מתחברים ב-`POST /api/auth/login`, מעתיקים את ה-`token` ומדביקים בכפתור **Authorize**.

---

## מבנה הפרויקט (Clean Architecture)

```
ClinicQueue.Domain          ישויות ו-Enums, בלי תלות בשום חבילה
ClinicQueue.Application     Services, ממשקים, DTOs, AutoMapper Profile, Result
ClinicQueue.Infrastructure  DbContext, Migrations, Repositories, JWT, BCrypt
ClinicQueue.API             Controllers, Middleware, Program.cs, nlog.config
ClinicQueue.Tests           xUnit + Moq, ובדיקות concurrency
```

## Endpoints עיקריים

| Method | נתיב | הרשאה |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | פתוח |
| GET | `/api/doctors`, `/api/specializations` | מחובר |
| POST / PUT / DELETE | `/api/doctors/{id}` | Admin |
| GET | `/api/slots?doctorId=&specializationId=&date=&page=` | מחובר |
| POST / DELETE | `/api/slots/{id}` | Admin |
| POST | `/api/appointments` | Patient |
| GET | `/api/appointments/mine` | מחובר |
| GET | `/api/appointments` | Admin |
| DELETE | `/api/appointments/{id}` (ביטול) | בעל התור או Admin |

---

## הערות

- **טעינת נתונים קשורים:** במקום `Include`/`ThenInclude`, הרשימות נטענות עם `ProjectTo` של AutoMapper. כך נוצרת שאילתת SQL אחת שמביאה רק את העמודות הנדרשות, בלי N+1. `Include` משמש בפעולות עדכון.
- **בדיקות ה-concurrency** רצות על SQLite בזיכרון. מנגנון ה-Version הוא של EF עצמו, ולכן הוא לא תלוי ב-PostgreSQL.
- **AutoMapper 13.0.1:** לגרסה יש התראת אבטחה ידועה. התיקון קיים רק בגרסאות 15.1.1 ומעלה, שדורשות רישיון מסחרי, ולכן הוחלט להישאר בגרסה הזו לצורך פרויקט לימודי.
- **לוגים:** NLog כותב לקובץ `logs/` ולקונסול. כל בקשה נרשמת עם ה-CorrelationId שלה, בלי סיסמאות ובלי tokens.
