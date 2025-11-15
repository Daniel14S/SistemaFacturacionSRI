using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SistemaFacturacionSRI.WebUI.Middleware
{
    /// <summary>
    /// Middleware que estandariza las respuestas 401/403 para API y UI.
    /// </summary>
    public class AuthErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthErrorHandlingMiddleware> _logger;

        public AuthErrorHandlingMiddleware(RequestDelegate next, ILogger<AuthErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.HasStarted)
            {
                return;
            }

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized ||
                context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                await HandleAuthErrorAsync(context);
            }
        }

        private async Task HandleAuthErrorAsync(HttpContext context)
        {
            var statusCode = context.Response.StatusCode;
            var message = statusCode == StatusCodes.Status401Unauthorized
                ? "No autorizado. Inicia sesión para continuar."
                : "Acceso denegado. No tienes permisos suficientes.";

            _logger.LogWarning("{Method} {Path} terminó con {StatusCode} - {Message}",
                context.Request?.Method ?? "UNKNOWN",
                context.Request?.Path.Value ?? string.Empty,
                statusCode,
                message);

            var prefersJson = IsApiOrJsonRequest(context);

            context.Response.Headers.Clear();
            context.Response.StatusCode = statusCode; // reafirmar

            if (prefersJson)
            {
                context.Response.ContentType = "application/json";
                var payload = new
                {
                    success = false,
                    statusCode,
                    message,
                    path = context.Request?.Path.Value ?? string.Empty,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
            else
            {
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync($"Error {statusCode}: {message}");
            }
        }

        private static bool IsApiOrJsonRequest(HttpContext context)
        {
            if (context.Request?.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            if (context.Request?.Headers.TryGetValue("Accept", out var acceptHeaders) == true)
            {
                return acceptHeaders.Any(h =>
                    !string.IsNullOrWhiteSpace(h) &&
                    h.Contains("application/json", StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }

    public static class AuthErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthErrorHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthErrorHandlingMiddleware>();
        }
    }
}
