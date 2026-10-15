namespace ClinicQueue.Application.DTOs.Common;

/// <summary>
/// תשובה של רשימה מחולקת לעמודים - הפריטים של העמוד הנוכחי + נתונים לניווט.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    // סך כל הפריטים שעונים על הסינון (לא רק בעמוד הזה)
    public int TotalCount { get; set; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
