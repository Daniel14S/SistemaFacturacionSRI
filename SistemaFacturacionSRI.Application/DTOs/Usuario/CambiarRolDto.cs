using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Usuario
{
    /// <summary>
    /// DTO para cambiar el rol de un usuario.
    /// Operación restringida solo a administradores.
    /// </summary>
    public class CambiarRolDto
    {
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nuevo rol es obligatorio")]
        [Range(1, 2, ErrorMessage = "El rol debe ser Administrador (1) o Vendedor (2)")]
        public int NuevoRolId { get; set; }

        /// <summary>
        /// Motivo del cambio de rol (opcional, para auditoría)
        /// </summary>
        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string? Motivo { get; set; }
    }
}