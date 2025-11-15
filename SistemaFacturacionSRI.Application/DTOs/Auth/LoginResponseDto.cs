namespace SistemaFacturacionSRI.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para la respuesta del login
    /// Contiene el token JWT y la información del usuario autenticado
    /// </summary>
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UsuarioDto? Usuario { get; set; }
        
        /// <summary>
        /// Fecha de expiración del token
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}