using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation id
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        var method = context.Request.Method;
        var path = context.Request.Path;

        // Log entry
        _logger.LogInformation(
            "START Request {Method} {Path} CorrelationId={CorrelationId}",
            method, path, correlationId);

        var stopwatch = Stopwatch.StartNew();

        // Set header BEFORE next
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        await _next(context);

        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Log exit
        _logger.LogInformation(
            "END Request {Method} {Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs} CorrelationId={CorrelationId}",
            method, path, statusCode, elapsedMs, correlationId);
    }
}