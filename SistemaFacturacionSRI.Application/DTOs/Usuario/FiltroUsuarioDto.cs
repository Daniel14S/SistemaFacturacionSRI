using SistemaFacturacionSRI.Application.DTOs.Common;

namespace SistemaFacturacionSRI.Application.DTOs.Usuario
{
    /// <summary>
    /// DTO para filtrar y buscar usuarios con paginación.
    /// </summary>
    public class FiltroUsuarioDto : PaginacionDto
    {
        /// <summary>
        /// Búsqueda por username, email o nombre
        /// </summary>
        public string? Busqueda { get; set; }

        /// <summary>
        /// Filtrar por rol específico (null = todos)
        /// </summary>
        public int? RolId { get; set; }

        /// <summary>
        /// Filtrar por estado (null = todos, true = activos, false = inactivos)
        /// </summary>
        public bool? Estado { get; set; }

        /// <summary>
        /// Mostrar solo usuarios bloqueados
        /// </summary>
        public bool? SoloBloqueados { get; set; }
    }
}