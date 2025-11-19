using SistemaFacturacionSRI.Application.DTOs.Common;

namespace SistemaFacturacionSRI.Application.DTOs.Cliente
{
    /// <summary>
    /// DTO para filtrar y buscar clientes con paginación.
    /// </summary>
    public class FiltroClienteDto : PaginacionDto
    {
        /// <summary>
        /// Búsqueda por identificación, nombres o apellidos
        /// </summary>
        public string? Busqueda { get; set; }

        /// <summary>
        /// Filtrar por tipo de identificación específico
        /// </summary>
        public int? TipoIdentificacionId { get; set; }

        /// <summary>
        /// Filtrar por estado activo/inactivo
        /// </summary>
        public bool? Estado { get; set; }
    }
}