using SistemaFacturacionSRI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Contrato para acceder a los lotes almacenados en la base de datos.
    /// </summary>
    public interface ILoteRepository
    {
        Task<IEnumerable<Lote>> ObtenerTodosAsync();

        /// <summary>
        /// Persiste un nuevo lote en la base de datos.
        /// </summary>
        Task<Lote> CrearAsync(Lote lote);
    }
}
