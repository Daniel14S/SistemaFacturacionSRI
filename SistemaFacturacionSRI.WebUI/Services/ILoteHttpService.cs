using SistemaFacturacionSRI.Domain.DTOs.Lote;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public interface ILoteHttpService
    {
        Task<IEnumerable<LoteDto>> ObtenerTodosAsync();
        Task<LoteDto> CrearAsync(CrearLoteDto dto);
        Task<LoteDto?> ObtenerPorIdAsync(int id);
        Task<LoteDto> ActualizarAsync(ActualizarLoteDto dto);
        Task EliminarAsync(int id);
        Task<IEnumerable<LoteDto>> ObtenerLotesPorProductoAsync(int productoId);
    }
}
