using ClinicQueue.API.Extensions;
using ClinicQueue.Application.Common.Exceptions;

namespace ClinicQueue.API.Middleware;

/// <summary>
/// הראשון ב-pipeline: תופס כל חריגה שלא טופלה ומחזיר JSON באותו פורמט של שגיאות העסק.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // הלקוח ביטל את הבקשה - אין למי להחזיר תשובה
        }
        catch (ConcurrencyConflictException ex)
        {
            using (BeginCorrelationScope(context))
                _logger.LogWarning(ex, "התנגשות על משאב");
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            using (BeginCorrelationScope(context))
                _logger.LogError(ex, "חריגה לא צפויה");
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "אירעה שגיאה בלתי צפויה. נסי שוב מאוחר יותר.");
        }
    }

    // ה-scope של CorrelationIdMiddleware כבר נסגר כשהחריגה מגיעה לכאן, לכן פותחים אותו מחדש
    private IDisposable? BeginCorrelationScope(HttpContext context) =>
        _logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdMiddleware.ItemKey] = context.Items[CorrelationIdMiddleware.ItemKey] ?? string.Empty
        });

    private static Task WriteAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.Clear();
        // Clear() מוחק גם את ה-header של ה-CorrelationId, לכן מחזירים אותו
        if (context.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
            context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ErrorResponse(status, message));
    }
}
