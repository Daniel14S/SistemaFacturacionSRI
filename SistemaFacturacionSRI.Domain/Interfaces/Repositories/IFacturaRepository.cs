using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para la entidad Factura
    /// </summary>
    public interface IFacturaRepository
    {
        /// <summary>
        /// Crea una nueva factura
        /// </summary>
        Task<Factura> CrearAsync(Factura factura);

        /// <summary>
        /// Obtiene una factura por su ID
        /// </summary>
        Task<Factura?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene una factura por su clave de acceso
        /// </summary>
        Task<Factura?> ObtenerPorClaveAccesoAsync(string claveAcceso);

        /// <summary>
        /// Obtiene una factura con todos sus detalles (includes)
        /// </summary>
        Task<Factura?> ObtenerConDetallesCompletosAsync(int id);

        /// <summary>
        /// Obtiene una factura con sus detalles por clave de acceso
        /// </summary>
        Task<Factura?> ObtenerConDetallesPorClaveAccesoAsync(string claveAcceso);

        /// <summary>
        /// Lista todas las facturas con paginación
        /// </summary>
        Task<(List<Factura> facturas, int total)> ListarAsync(int pagina, int tamanoPagina);

        /// <summary>
        /// Lista facturas con filtros
        /// </summary>
        Task<(List<Factura> facturas, int total)> ListarConFiltrosAsync(
            int? clienteId = null,
            int? usuarioId = null,
            EstadoFactura? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? numeroFactura = null,
            int pagina = 1,
            int tamanoPagina = 10);

        /// <summary>
        /// Obtiene facturas por cliente
        /// </summary>
        Task<List<Factura>> ObtenerPorClienteAsync(int clienteId);

        /// <summary>
        /// Obtiene facturas por usuario emisor
        /// </summary>
        Task<List<Factura>> ObtenerPorUsuarioAsync(int usuarioId);

        /// <summary>
        /// Obtiene facturas por estado
        /// </summary>
        Task<List<Factura>> ObtenerPorEstadoAsync(EstadoFactura estado);

        /// <summary>
        /// Obtiene facturas por rango de fechas
        /// </summary>
        Task<List<Factura>> ObtenerPorRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta);

        /// <summary>
        /// Actualiza una factura existente
        /// </summary>
        Task ActualizarAsync(Factura factura);

        /// <summary>
        /// Actualiza solo el estado de una factura (optimizado)
        /// </summary>
        Task ActualizarEstadoAsync(int id, EstadoFactura nuevoEstado);

        /// <summary>
        /// Actualiza los campos relacionados con la respuesta del SRI
        /// </summary>
        Task ActualizarRespuestaSRIAsync(
            int id,
            string? numeroAutorizacion,
            DateTime? fechaAutorizacion,
            string? xmlPath,
            string? xmlFirmadoPath,
            string? pdfPath,
            string? mensajesSRI);

        /// <summary>
        /// Elimina una factura (soft delete)
        /// </summary>
        Task EliminarAsync(int id);

        /// <summary>
        /// Verifica si existe una factura con un número determinado
        /// </summary>
        Task<bool> ExisteNumeroFacturaAsync(string numeroFactura);

        /// <summary>
        /// Verifica si existe una factura con una clave de acceso determinada
        /// </summary>
        Task<bool> ExisteClaveAccesoAsync(string claveAcceso);

        /// <summary>
        /// Obtiene el total de facturas por estado
        /// </summary>
        Task<int> ContarPorEstadoAsync(EstadoFactura estado);

        /// <summary>
        /// Obtiene el total facturado en un rango de fechas
        /// </summary>
        Task<decimal> ObtenerTotalFacturadoAsync(DateTime fechaDesde, DateTime fechaHasta);

        /// <summary>
        /// Obtiene las últimas N facturas
        /// </summary>
        Task<List<Factura>> ObtenerUltimasAsync(int cantidad);
    }
}
