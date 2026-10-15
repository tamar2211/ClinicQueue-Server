using System.ComponentModel.DataAnnotations;

namespace ClinicQueue.Core.DTOs.Common;

/// <summary>
/// פרמטרים לחלוקה לעמודים. מגיעים מה-URL: ?page=2&pageSize=10
/// </summary>
public class PaginationQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    [Range(1, int.MaxValue, ErrorMessage = "מספר העמוד חייב להיות 1 ומעלה")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "גודל העמוד חייב להיות בין 1 ל-50")]
    public int PageSize { get; set; } = DefaultPageSize;
}
