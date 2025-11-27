using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Operaciones de consulta para facturas.
    /// </summary>
    public interface IFacturaService
    {
        /// <summary>
        /// Lista facturas con filtros dinámicos y paginación.
        /// </summary>
        Task<PagedResultDto<Factura>> ListarFacturasAsync(FiltroFacturaDto filtro, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una factura completa (cliente, usuario, detalles e info adicional).
        /// </summary>
        Task<Factura?> ObtenerPorIdAsync(int facturaId, CancellationToken cancellationToken = default);
    }
}
