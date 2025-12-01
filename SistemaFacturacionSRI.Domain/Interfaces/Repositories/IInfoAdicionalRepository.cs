using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para la entidad InfoAdicional
    /// </summary>
    public interface IInfoAdicionalRepository
    {
        /// <summary>
        /// Crea una nueva información adicional
        /// </summary>
        Task<InfoAdicional> CrearAsync(InfoAdicional infoAdicional);

        /// <summary>
        /// Crea múltiples registros de información adicional
        /// </summary>
        Task CrearVariosAsync(List<InfoAdicional> infosAdicionales);

        /// <summary>
        /// Obtiene una información adicional por su ID
        /// </summary>
        Task<InfoAdicional?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene todas las informaciones adicionales de una factura
        /// </summary>
        Task<List<InfoAdicional>> ObtenerPorFacturaIdAsync(int facturaId);

        /// <summary>
        /// Actualiza una información adicional
        /// </summary>
        Task ActualizarAsync(InfoAdicional infoAdicional);

        /// <summary>
        /// Elimina una información adicional
        /// </summary>
        Task EliminarAsync(int id);

        /// <summary>
        /// Elimina todas las informaciones adicionales de una factura
        /// </summary>
        Task EliminarPorFacturaIdAsync(int facturaId);
    }
}