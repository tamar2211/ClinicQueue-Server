using ClinicQueue.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ClinicQueue.API.Extensions;

/// הפורמט האחיד לכל שגיאה. Errors מלא רק בשגיאות ולידציה (שדה ← הודעות)
public record ErrorResponse(
    int Status,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IDictionary<string, string[]>? Errors = null);

public static class ResultExtensions
{
    /// ממיר Result של ה-Service לסטטוס HTTP: הצלחה = 200, שגיאה לפי ErrorType.
    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : ToError(result, controller);

    /// בלי ערך מוחזר (עדכון/מחיקה): הצלחה = 204
    public static ActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : ToError(result, controller);

    private static ObjectResult ToError(Result result, ControllerBase controller)
    {
        var status = result.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        return controller.StatusCode(status, new ErrorResponse(status, result.ErrorMessage ?? string.Empty));
    }
}
