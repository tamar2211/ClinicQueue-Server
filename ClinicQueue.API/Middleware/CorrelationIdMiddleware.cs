namespace ClinicQueue.API.Middleware;

/// <summary>
/// מזהה ייחודי לכל בקשה: נשלח ללקוח ב-header, נכנס ל-scope של הלוגים, ומאפשר לשחזר בקשה שלמה מתוך הלוג.
/// תמיד נוצר בשרת (לא מקבלים מהלקוח) כדי שאי אפשר יהיה להזריק ללוג ערכים זרים.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            // רק method ו-path: בלי query string ובלי body (עלולים להכיל סיסמאות או tokens)
            _logger.LogInformation("{Method} {Path}", context.Request.Method, context.Request.Path);
            await _next(context);
        }
    }
}
