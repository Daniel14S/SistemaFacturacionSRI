// SistemaFacturacionSRI.Application/Interfaces/Services/ILoteService.cs
using SistemaFacturacionSRI.Application.DTOs.Lote;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    public interface ILoteService
    {
        Task<IEnumerable<LoteDto>> ObtenerTodosAsync();
        Task<LoteDto> CrearAsync(CrearLoteDto dto);
        Task<LoteDto?> ObtenerLotePrioritarioAsync(int idProducto);
        Task<IEnumerable<LoteDto>> ObtenerPorProductoAsync(int productoId);
        
        //  NUEVOS MÉTODOS
        Task<LoteDto?> ObtenerPorIdAsync(int loteId);
        Task<LoteDto> ActualizarAsync(ActualizarLoteDto dto);
        Task EliminarAsync(int loteId);
        
    }
}