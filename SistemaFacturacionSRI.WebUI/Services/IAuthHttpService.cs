using SistemaFacturacionSRI.Domain.DTOs.Auth;

namespace SistemaFacturacionSRI.WebUI.Services
{
    /// <summary>
    /// Servicio HTTP para operar con los endpoints de autenticación.
    /// </summary>
    public interface IAuthHttpService
    {
        /// <summary>
        /// Solicita el login en la API y devuelve la respuesta recibida.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="password">Contraseña.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        Task<LoginResponseDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoca el endpoint de logout para invalidar la sesión actual.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}
