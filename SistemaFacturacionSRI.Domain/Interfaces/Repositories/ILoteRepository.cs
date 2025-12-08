// SistemaFacturacionSRI.Application/Interfaces/Repositories/ILoteRepository.cs
using SistemaFacturacionSRI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    public interface ILoteRepository
    {
        Task<IEnumerable<Lote>> ObtenerTodosAsync();
        Task<Lote> CrearAsync(Lote lote);
        Task<IEnumerable<Lote>> ObtenerLotesPorProductoAsync(int idProducto);
        Task<IEnumerable<Lote>> ObtenerPorProductoAsync(int productoId);
        
        // NUEVOS MÉTODOS
        Task<Lote?> ObtenerPorIdAsync(int loteId);
        Task ActualizarAsync(Lote lote);
        Task EliminarAsync(int loteId);
        Task ActualizarPVPDeLotesPorProductoAsync(int productoId, decimal nuevoPVP, int? loteExcluidoId = null);
        
        /// <summary>
        /// Reduce la cantidad disponible de un lote específico.
        /// </summary>
        /// <param name="loteId">ID del lote</param>
        /// <param name="cantidad">Cantidad a reducir</param>
        Task ReducirStockAsync(int loteId, decimal cantidad);
    }
}