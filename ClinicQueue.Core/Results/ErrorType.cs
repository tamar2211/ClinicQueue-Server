namespace ClinicQueue.Core.Results;

/// <summary>
/// סוג השגיאה העסקית. ה-Controller ממיר אותו לסטטוס HTTP:
/// Validation=400, Unauthorized=401, Forbidden=403, NotFound=404, Conflict=409.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}
