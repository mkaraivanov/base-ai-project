namespace Backend.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME-sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // XSS Protection
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Content Security Policy
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");

        // Referrer Policy
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=()");

        await _next(context);
    }
}
