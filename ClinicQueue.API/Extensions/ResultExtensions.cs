using ClinicQueue.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueue.API.Extensions;

public record ErrorResponse(int Status, string Message);

public static class ResultExtensions
{
    /// ממיר Result של ה-Service לסטטוס HTTP: הצלחה = 200, שגיאה לפי ErrorType.
    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess) return controller.Ok(result.Value);

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
