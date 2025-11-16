using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Application.Security;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Middleware
{
    /// <summary>
    /// Middleware que valida y procesa tokens JWT en cada petición HTTP.
    /// Extrae el token del header Authorization y adjunta los claims al contexto.
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Procesa cada petición HTTP y valida el token JWT si está presente
        /// </summary>
        public async Task InvokeAsync(HttpContext context, JwtTokenGenerator jwtTokenGenerator)
        {
            // 1. Intentar extraer el token del header Authorization
            var token = ExtraerTokenDelHeader(context);

            if (!string.IsNullOrEmpty(token))
            {
                // 2. Validar el token
                var tokenValido = jwtTokenGenerator.ValidateToken(token);

                if (tokenValido)
                {
                    // 3. Extraer los claims del token
                    var claims = jwtTokenGenerator.GetClaimsFromToken(token);

                    if (claims != null && claims.Any())
                    {
                        // 4. Crear una identidad de claims
                        var claimsIdentity = new ClaimsIdentity(claims, "jwt");
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                        // 5. Adjuntar el usuario al contexto HTTP
                        context.User = claimsPrincipal;

                        // Log para debugging (opcional, comentar en producción)
                        var userId = jwtTokenGenerator.GetUsuarioIdFromToken(token);
                        var role = jwtTokenGenerator.GetRolFromToken(token);
                        _logger.LogDebug("Usuario autenticado: ID={UserId}, Rol={Role}", userId, role);
                    }
                    else
                    {
                        _logger.LogWarning("Token válido pero sin claims: {Token}", token.Substring(0, Math.Min(20, token.Length)));
                    }
                }
                else
                {
                    // Token inválido o expirado
                    _logger.LogWarning("Token JWT inválido o expirado en {Path}", context.Request.Path);
                    
                    // Agregar header indicando que el token es inválido
                    context.Response.Headers.Add("Token-Invalid", "true");
                }
            }

            // 6. Continuar con el siguiente middleware en el pipeline
            await _next(context);
        }

        /// <summary>
        /// Extrae el token JWT del header Authorization
        /// Soporta formato: "Bearer {token}"
        /// </summary>
        private string? ExtraerTokenDelHeader(HttpContext context)
        {
            // Obtener el header Authorization
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return null;
            }

            // Verificar que tenga el formato "Bearer {token}"
            if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Extraer el token (después de "Bearer ")
                return authorizationHeader.Substring("Bearer ".Length).Trim();
            }

            // Si no tiene el formato correcto, intentar usarlo directamente
            // (por si el cliente envió solo el token sin "Bearer")
            return authorizationHeader.Trim();
        }
    }

    /// <summary>
    /// Clase de extensión para registrar el middleware en el pipeline
    /// </summary>
    public static class JwtMiddlewareExtensions
    {
        /// <summary>
        /// Agrega el JwtMiddleware al pipeline de la aplicación
        /// </summary>
        public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwtMiddleware>();
        }
    }
}