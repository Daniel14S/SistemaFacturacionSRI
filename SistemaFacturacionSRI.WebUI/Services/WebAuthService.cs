using SistemaFacturacionSRI.Application.DTOs.Auth;
using SistemaFacturacionSRI.Application.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio de autenticación para la interfaz web (Blazor).
    /// </summary>
    public class WebAuthService
    {
        private readonly IAuthService _authService;
        private readonly CustomAuthenticationStateProvider _authStateProvider;
        private readonly ILogger<WebAuthService> _logger;

        public WebAuthService(
            IAuthService authService,
            AuthenticationStateProvider authStateProvider,
            ILogger<WebAuthService> logger)
        {
            _authService = authService;
            _authStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
            _logger = logger;
        }

        /// <summary>
        /// Inicia sesión con username y password.
        /// </summary>
        public async Task<LoginResponseDto> Login(string username, string password)
        {
            var request = new LoginRequestDto
            {
                Username = username,
                Password = password
            };

            var response = await _authService.LoginAsync(request);

            if (response.Success && !string.IsNullOrEmpty(response.Token))
            {
                // Marcar usuario como autenticado
                _authStateProvider.MarkUserAsAuthenticated(response.Token);
                _logger.LogInformation("Usuario {Username} autenticado exitosamente", username);
            }

            return response;
        }

        /// <summary>
/// Cierra sesión del usuario actual.
/// </summary>
public async Task Logout()
{
    try
    {
        // Intentar logout en el servidor (actualizar último acceso)
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
        {
            try
            {
                await _authService.LogoutAsync(userIdInt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al hacer logout en el servidor, continuando con logout local");
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error al obtener estado de autenticación");
    }
    finally
    {
        // SIEMPRE limpiar el estado local, pase lo que pase
        _authStateProvider.MarkUserAsLoggedOut();
        _logger.LogInformation("Usuario cerró sesión");
    }
}


        /// <summary>
        /// Verifica si el usuario está autenticado.
        /// </summary>
        public async Task<bool> IsAuthenticated()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// Obtiene el usuario autenticado actual.
        /// </summary>
        public async Task<System.Security.Claims.ClaimsPrincipal> GetCurrentUser()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User;
        }

        /// <summary>
        /// Obtiene el rol del usuario autenticado.
        /// </summary>
        public async Task<string?> GetUserRole()
        {
            var user = await GetCurrentUser();
            return user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico.
        /// </summary>
        public async Task<bool> IsInRole(string role)
        {
            var user = await GetCurrentUser();
            return user.IsInRole(role);
        }
    }
}