# לב הפרויקט – תחרות על משבצת (הסבר + חומר ל-README)

## המשאב והבעיה
המשאב המוגבל: `DoctorSlot` (משבצת זמן אצל רופא). שתי מטופלות לוחצות "קבעי תור" על אותה משבצת באותו רגע.
בלי הגנה: שתיהן קוראות `IsBooked = false`, שתיהן עוברות את הבדיקה, שתיהן שומרות – ונוצרים שני תורים על משבצת אחת.

## שלוש שכבות הגנה (מהחשובה לפחות חשובה)
1. **Optimistic concurrency על המשבצת** – שדה `Guid Version`, מוגדר כ-`IsConcurrencyToken()` ב-Fluent API (`AppDbContext`).
   `SaveChangesAsync` (דריסה ב-`AppDbContext`) מחליף את ה-Version בכל שינוי. EF מוסיף את הערך הישן ל-`WHERE` של ה-UPDATE.
   המטופלת השנייה: אף שורה לא מתעדכנת ← `DbUpdateConcurrencyException`.
2. **המרה ל-409** – `UnitOfWork` (Infrastructure) תופס רק `DbUpdateConcurrencyException` וזורק `ConcurrencyConflictException` (Application).
   ה-Service מחזיר `Result.Conflict`, ה-Controller מחזיר **409**, ו-NLog רושם **Warning**.
   למה החריגה שלנו: ה-Service לא צריך להכיר את EF ← שכבות נקיות.
3. **אינדקס ייחודי חלקי ב-DB** (migration `UniqueActiveAppointmentPerSlot`) – `UNIQUE (DoctorSlotId) WHERE Status = 'Active'`.
   גם אם ה-Version נעקף, ה-DB עצמו מונע שני תורים פעילים על משבצת. הפרת האינדקס ← `DbUpdateException` (Postgres 23505) ← `UnitOfWork` ממיר גם אותה ל-`ConcurrencyConflictException` ← 409.
   ביטול תור = שינוי Status, לכן התור המבוטל יוצא מהאינדקס והמשבצת מתפנה.

## החור של אותה מטופלת, וסגירתו (Version גם על User)
שתי בקשות מקבילות של **אותה מטופלת** על **משבצות חופפות** של רופאים שונים: כל משבצת מעודכנת בנפרד ← ה-Version של כל אחת תקין ← שתיהן היו עוברות.
הפתרון: ל-`User` יש גם `Version` (concurrency token). כל קביעת תור "נוגעת" בשורת המשתמשת (`IUserRepository.Touch`), ולכן שתי קביעות מקבילות של אותה מטופלת מתנגשות על שורת המשתמשת ← 409.
הבדיקה הרגילה `HasActiveAppointmentAtAsync` נשארת למקרה הרגיל (לא מקבילי), ומחזירה הודעה ברורה.
זו אותה טכניקה (optimistic) שהמורה דורשת, ללא תלות בהרחבות של PostgreSQL.

## הבדיקות שמוכיחות את זה (`ClinicQueue.Tests/ConcurrencyTests.cs`, SQLite בזיכרון)
- שני DbContext על אותה משבצת: הראשון נשמר, השני `DbUpdateConcurrencyException`.
- `UnitOfWork` ממיר ל-`ConcurrencyConflictException`.
- אותה מטופלת, שתי משבצות שונות: השני נדחה (Version של User).
- שני תורים פעילים על משבצת אחת: האינדקס החלקי דוחה; תור מבוטל לא חוסם.
- אימות שהבדיקות אמיתיות: ביטול זמני של `IsConcurrencyToken` על המשבצת גרם לכישלון שתי הבדיקות הראשונות.

## מה להסביר בעל פה
- למה optimistic ולא pessimistic: בלי נעילות והמתנות, ההתנגשות נדירה, והמחיר רק שימוש חוזר בבקשה.
- למה Version ולא xmin: עובד על כל DB, קל להבנה, וניהול מרוכז ב-`SaveChangesAsync`.
- למה 409 ולא 400/500: הבקשה תקינה, אבל מתנגשת עם מצב המשאב.
- הבדיקה: שני `DbContext` נפרדים טוענים אותה משבצת, שניהם משנים, הראשון נשמר והשני מקבל חריגה (חובה לפי המורה).
