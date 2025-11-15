namespace SistemaFacturacionSRI.Application.DTOs.Auth
{
    /// <summary>
    /// DTO con información segura del usuario (sin contraseña)
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        
        /// <summary>
        /// Nombre completo del usuario (si existe)
        /// </summary>
        public string? NombreCompleto { get; set; }
    }
}