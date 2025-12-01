using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para la entidad DetalleFactura
    /// </summary>
    public interface IDetalleFacturaRepository
    {
        /// <summary>
        /// Crea un nuevo detalle de factura
        /// </summary>
        Task<DetalleFactura> CrearAsync(DetalleFactura detalle);

        /// <summary>
        /// Crea múltiples detalles de factura
        /// </summary>
        Task CrearVariosAsync(List<DetalleFactura> detalles);

        /// <summary>
        /// Obtiene un detalle por su ID
        /// </summary>
        Task<DetalleFactura?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene todos los detalles de una factura
        /// </summary>
        Task<List<DetalleFactura>> ObtenerPorFacturaIdAsync(int facturaId);

        /// <summary>
        /// Actualiza un detalle de factura
        /// </summary>
        Task ActualizarAsync(DetalleFactura detalle);

        /// <summary>
        /// Elimina un detalle de factura
        /// </summary>
        Task EliminarAsync(int id);

        /// <summary>
        /// Elimina todos los detalles de una factura
        /// </summary>
        Task EliminarPorFacturaIdAsync(int facturaId);
    }
}