using SistemaFacturacionSRI.Application.DTOs.Auth;

namespace SistemaFacturacionSRI.Application.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de autenticación
    /// Define el contrato de operaciones de login, logout y validación de tokens
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario con sus credenciales
        /// </summary>
        /// <param name="request">Credenciales del usuario (username y password)</param>
        /// <returns>Respuesta con token JWT si las credenciales son válidas</returns>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Cierra la sesión del usuario y actualiza su último acceso
        /// </summary>
        /// <param name="usuarioId">ID del usuario que cierra sesión</param>
        /// <returns>True si se cerró sesión correctamente</returns>
        Task<bool> LogoutAsync(int usuarioId);

        /// <summary>
        /// Valida si un token JWT es válido y no ha expirado
        /// </summary>
        /// <param name="token">Token JWT a validar</param>
        /// <returns>True si el token es válido</returns>
        Task<bool> ValidarTokenAsync(string token);

        /// <summary>
        /// Obtiene el ID del usuario desde un token JWT
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>ID del usuario o null si el token es inválido</returns>
        Task<int?> ObtenerUsuarioIdDesdeTokenAsync(string token);
    }
}