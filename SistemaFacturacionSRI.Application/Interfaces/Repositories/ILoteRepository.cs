using SistemaFacturacionSRI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para acceder a los lotes almacenados en la base de datos.
    /// Define los métodos que cualquier repositorio de lotes debe implementar.
    /// </summary>
    public interface ILoteRepository
    {
        /// <summary>
        /// Obtiene todos los lotes registrados en la base de datos.
        /// </summary>
        Task<IEnumerable<Lote>> ObtenerTodosAsync();

        /// <summary>
        /// Persiste un nuevo lote en la base de datos.
        /// </summary>
        Task<Lote> CrearAsync(Lote lote);

        /// <summary>
        /// Obtiene todos los lotes asociados a un producto específico.
        /// </summary>
        Task<IEnumerable<Lote>> ObtenerLotesPorProductoAsync(int idProducto);
        Task<IEnumerable<Lote>> ObtenerPorProductoAsync(int productoId);

    }
}
