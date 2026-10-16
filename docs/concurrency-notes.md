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
   גם אם ה-Version נעקף, ה-DB עצמו מונע שני תורים פעילים על משבצת. הפרת האינדקס ← `DbUpdateException` ← גם היא מומרת ל-409 (לטפל ב-UnitOfWork).
   ביטול תור = שינוי Status, לכן התור המבוטל יוצא מהאינדקס והמשבצת מתפנה.

## החור שהשתיים הראשונות לא סוגרות
שתי בקשות מקבילות של **אותה מטופלת** על **משבצות חופפות** של רופאים שונים: כל משבצת מעודכנת בנפרד ← ה-Version של כל אחת תקין ← שתיהן עוברות.
טיפול (ב-`AppointmentService`): `HasActiveAppointmentAtAsync` לפני השמירה (409 למקרה הרגיל). המקרה המקבילי באמת – להחליט בהמשך (אינדקס/טרנזקציה).

## מה להסביר בעל פה
- למה optimistic ולא pessimistic: בלי נעילות והמתנות, ההתנגשות נדירה, והמחיר רק שימוש חוזר בבקשה.
- למה Version ולא xmin: עובד על כל DB, קל להבנה, וניהול מרוכז ב-`SaveChangesAsync`.
- למה 409 ולא 400/500: הבקשה תקינה, אבל מתנגשת עם מצב המשאב.
- הבדיקה: שני `DbContext` נפרדים טוענים אותה משבצת, שניהם משנים, הראשון נשמר והשני מקבל חריגה (חובה לפי המורה).
