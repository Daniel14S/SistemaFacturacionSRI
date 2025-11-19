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
        private readonly IAutoLoginService _autoLoginService;
        private readonly ILogger<AuthHeaderHandler> _logger;

        public AuthHeaderHandler(
            ITokenStorage tokenStorage,
            IAutoLoginService autoLoginService,
            ILogger<AuthHeaderHandler> logger)
        {
            _tokenStorage = tokenStorage;
            _autoLoginService = autoLoginService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await _autoLoginService.EnsureAdminTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo asegurar el token antes de la petición a {RequestUri}", 
                    request.RequestUri);
            }

            AttachToken(request);

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

            // 4. Manejar respuestas de autenticación
            await HandleAuthenticationResponse(response, request);

            return response;
        }

        private void AttachToken(HttpRequestMessage request)
        {
            var token = _tokenStorage.Token;
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No hay token disponible para adjuntar a {RequestUri}", request.RequestUri);
                return;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogDebug("Token JWT adjuntado a la petición {Method} {RequestUri}", 
                request.Method, 
                request.RequestUri);

        }

        private async Task HandleAuthenticationResponse(HttpResponseMessage response, HttpRequestMessage request)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: // 401
                    _logger.LogWarning("Respuesta 401 Unauthorized de {RequestUri}. Token inválido o expirado.", 
                        request.RequestUri);
                    
                    // Verificar si el token expiró
                    if (response.Headers.Contains("Token-Expired"))
                    {
                        _logger.LogInformation("Token expirado detectado. Limpiando token storage.");
                        _tokenStorage.Clear();
                        
                        // Opcional: Intentar refrescar automáticamente
                        try
                        {
                            await _autoLoginService.ForceRefreshTokenAsync();
                            _logger.LogInformation("Token refrescado automáticamente después de expiración.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "No se pudo refrescar el token automáticamente.");
                        }
                    }
                    else
                    {
                        // Token inválido (no solo expirado)
                        _tokenStorage.Clear();
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
