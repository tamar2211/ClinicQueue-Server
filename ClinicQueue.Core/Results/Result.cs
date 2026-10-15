namespace ClinicQueue.Core.Results;

/// <summary>
/// תוצאה של פעולה ב-Service, בלי ערך מוחזר (למשל: ביטול תור).
/// במקום לזרוק חריגה על כל מקרה צפוי ("התור תפוס", "לא נמצא"),
/// ה-Service מחזיר Result עם סוג השגיאה והודעה ברורה.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public ErrorType ErrorType { get; }
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, ErrorType errorType, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new(true, ErrorType.None, null);

    public static Result Fail(ErrorType type, string message) => new(false, type, message);

    public static Result Validation(string message) => Fail(ErrorType.Validation, message);
    public static Result Unauthorized(string message) => Fail(ErrorType.Unauthorized, message);
    public static Result Forbidden(string message) => Fail(ErrorType.Forbidden, message);
    public static Result NotFound(string message) => Fail(ErrorType.NotFound, message);
    public static Result Conflict(string message) => Fail(ErrorType.Conflict, message);
}

/// <summary>
/// תוצאה עם ערך מוחזר (למשל: התור שנוצר).
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, ErrorType errorType, string? errorMessage)
        : base(isSuccess, errorType, errorMessage)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, ErrorType.None, null);

    public static new Result<T> Fail(ErrorType type, string message) => new(false, default, type, message);

    public static new Result<T> Validation(string message) => Fail(ErrorType.Validation, message);
    public static new Result<T> Unauthorized(string message) => Fail(ErrorType.Unauthorized, message);
    public static new Result<T> Forbidden(string message) => Fail(ErrorType.Forbidden, message);
    public static new Result<T> NotFound(string message) => Fail(ErrorType.NotFound, message);
    public static new Result<T> Conflict(string message) => Fail(ErrorType.Conflict, message);
}
