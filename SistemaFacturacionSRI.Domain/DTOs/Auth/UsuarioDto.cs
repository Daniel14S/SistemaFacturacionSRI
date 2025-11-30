namespace SistemaFacturacionSRI.Domain.DTOs.Auth
{
    /// <summary>
    /// DTO con información segura del usuario (sin contraseña)
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nombre1 { get; set; } = string.Empty;
        public string? Nombre2 { get; set; }
        public string Apellido1 { get; set; } = string.Empty;
        public string? Apellido2 { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int RolId { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        
        /// <summary>
        /// Nombre completo del usuario (si existe)
        /// </summary>
        public string? NombreCompleto { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }
}