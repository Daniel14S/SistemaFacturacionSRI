namespace SistemaFacturacionSRI.Domain.DTOs.Usuario
{
    /// <summary>
    /// DTO simplificado para listar usuarios.
    /// Usado en tablas y listas donde no se necesita toda la información.
    /// </summary>
    public class UsuarioListDto
    {
        public int UsuarioId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int RolId { get; set; }
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        /// <summary>
        /// Indica si el usuario está bloqueado por intentos fallidos
        /// </summary>
        public bool EstaBloqueado { get; set; }

        /// <summary>
        /// Número de intentos de login fallidos
        /// </summary>
        public int IntentosLogin { get; set; }

        /// <summary>
        /// Etiqueta visual del estado (Activo, Inactivo, Bloqueado)
        /// </summary>
        public string EstadoTexto => Estado 
            ? (EstaBloqueado ? "Bloqueado" : "Activo") 
            : "Inactivo";

        /// <summary>
        /// Color para mostrar en la UI (Bootstrap classes)
        /// </summary>
        public string EstadoColor => Estado
            ? (EstaBloqueado ? "warning" : "success")
            : "danger";
    }
}