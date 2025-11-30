using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.DTOs.Factura
{
    /// <summary>
    /// Parámetros de filtrado y paginación para consultar facturas.
    /// </summary>
    public class FiltroFacturaDto : PaginacionDto
    {
        public int? ClienteId { get; set; }
        public int? UsuarioId { get; set; }
        public EstadoFactura? Estado { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? NumeroFactura { get; set; }
        public string? ClaveAcceso { get; set; }
        public string? TextoBusqueda { get; set; }
    }
}
