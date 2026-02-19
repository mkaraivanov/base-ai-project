using System.Text.RegularExpressions;

namespace Backend.Middleware;

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    private static readonly Regex[] SqlPatterns = CreatePatterns(
        @"(\bOR\b|\bAND\b).*=.*",
        @"(';|"";|--|\#|\/\*|\*\/)",
        @"\bDROP\b.*\bTABLE\b",
        @"\bEXEC\b|\bEXECUTE\b",
        @"\bUNION\b.*\bSELECT\b"
    );

    private static readonly Regex[] XssPatterns = CreatePatterns(
        @"<script[^>]*>.*?</script>",
        @"javascript:",
        @"on\w+\s*=",
        @"<iframe[^>]*>",
        @"<object[^>]*>"
    );

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        foreach (var query in context.Request.Query)
        {
            if (ContainsSqlInjection(query.Value.ToString()))
            {
                _logger.LogWarning("Potential SQL injection detected in query: {Key}", query.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid input detected" });
                return;
            }
        }

        foreach (var header in context.Request.Headers)
        {
            if (ContainsXss(header.Value.ToString()))
            {
                _logger.LogWarning("Potential XSS detected in header: {Key}", header.Key);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid input detected" });
                return;
            }
        }

        await _next(context);
    }

    private static bool ContainsSqlInjection(string input) =>
        !string.IsNullOrEmpty(input) && SqlPatterns.Any(p => p.IsMatch(input));

    private static bool ContainsXss(string input) =>
        !string.IsNullOrEmpty(input) && XssPatterns.Any(p => p.IsMatch(input));

    private static Regex[] CreatePatterns(params string[] patterns) =>
        patterns.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray();
}
