using SistemaFacturacionSRI.Application.DTOs.Lote;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Define operaciones de lectura sobre los lotes.
    /// </summary>
    public interface ILoteService
    {
        Task<IEnumerable<LoteDto>> ObtenerTodosAsync();

        /// <summary>
        /// Crea un nuevo lote asociado a un producto existente.
        /// </summary>
        Task<LoteDto> CrearAsync(CrearLoteDto dto);

        /// <summary>
        /// Devuelve el lote prioritario de un producto según la fecha de expiración y stock disponible.
        /// </summary>
        Task<LoteDto?> ObtenerLotePrioritarioAsync(int idProducto);
        Task<IEnumerable<LoteDto>> ObtenerPorProductoAsync(int productoId);

    }
}
