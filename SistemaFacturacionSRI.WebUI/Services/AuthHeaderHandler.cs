using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly ILogger<AuthHeaderHandler> _logger;

        public AuthHeaderHandler(
            ITokenStorage tokenStorage,
            ILogger<AuthHeaderHandler> logger)
        {
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // 1. Adjuntar token si existe
            AttachTokenIfAvailable(request);

            // 2. Enviar petición
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión al enviar petición a {RequestUri}", request.RequestUri);
                throw;
            }

            // 3. Manejar respuestas de autenticación
            HandleAuthenticationResponse(response, request);

            return response;
        }

        private void AttachTokenIfAvailable(HttpRequestMessage request)
        {
            // ✅ Leer token directamente de la propiedad
            var token = _tokenStorage.Token;
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("⚠️ No hay token disponible para adjuntar a {RequestUri}", request.RequestUri);
                return;
            }

            // Verificar si el token ha expirado
            if (_tokenStorage.TokenExpiresAt.HasValue && _tokenStorage.TokenExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning("Token expirado detectado. No se adjuntará a la petición.");
                _tokenStorage.Clear();
                return;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("✅ Token JWT adjuntado a la petición {Method} {RequestUri}", 
                request.Method, 
                request.RequestUri);
        }

        private void HandleAuthenticationResponse(HttpResponseMessage response, HttpRequestMessage request)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    _logger.LogWarning("Respuesta 401 Unauthorized de {RequestUri}. Token inválido o expirado.", 
                        request.RequestUri);
                    
                    _tokenStorage.Clear();
                    
                    if (response.Headers.Contains("Token-Expired"))
                    {
                        _logger.LogInformation("Token expirado detectado en respuesta.");
                    }
                    break;

                case HttpStatusCode.Forbidden: // 403
                    _logger.LogWarning("Respuesta 403 Forbidden de {RequestUri}. Usuario no tiene permisos suficientes.", 
                        request.RequestUri);
                    break;
            }
        }
    }
}