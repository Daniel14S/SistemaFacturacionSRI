using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
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

        /// <summary>
        /// Cambia el estado de una factura validando las transiciones permitidas.
        /// También registra la fecha del cambio y, si aplica, la fecha de autorización.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura.</param>
        /// <param name="nuevoEstado">Estado al que se desea mover la factura.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Factura actualizada con el nuevo estado.</returns>
        /// <exception cref="KeyNotFoundException">Si la factura no existe.</exception>
        /// <exception cref="InvalidOperationException">Si la transición no está permitida.</exception>
        Task<Factura> ActualizarEstadoAsync(int facturaId, EstadoFactura nuevoEstado, CancellationToken cancellationToken = default);

        /// <summary>
        /// Anula una factura previamente autorizada validando que el usuario tenga permisos.
        /// </summary>
        /// <param name="facturaId">Identificador de la factura a anular.</param>
        /// <param name="usuarioId">Usuario que solicita la anulación (se valida que sea administrador).</param>
        /// <param name="motivo">Motivo opcional para registrar en las observaciones.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Factura en estado ANULADA.</returns>
        /// <exception cref="KeyNotFoundException">Si la factura o el usuario no existen.</exception>
        /// <exception cref="InvalidOperationException">Si la factura no está autorizada.</exception>
        /// <exception cref="UnauthorizedAccessException">Si el usuario no tiene permisos suficientes.</exception>
        Task<Factura> AnularFacturaAsync(int facturaId, int usuarioId, string? motivo = null, CancellationToken cancellationToken = default);
    }
}
