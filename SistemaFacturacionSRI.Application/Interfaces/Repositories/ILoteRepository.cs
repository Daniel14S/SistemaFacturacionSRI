// SistemaFacturacionSRI.Application/Interfaces/Repositories/ILoteRepository.cs
using SistemaFacturacionSRI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
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
    }
}