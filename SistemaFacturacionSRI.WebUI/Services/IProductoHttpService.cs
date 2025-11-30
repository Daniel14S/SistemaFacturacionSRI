using SistemaFacturacionSRI.Domain.DTOs.Producto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public interface IProductoHttpService
    {
        Task<List<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
        Task<ProductoDto> CrearAsync(CrearProductoDto dto);
        Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto);
        Task EliminarAsync(int id);
        Task ReactivarAsync(int id);
        Task<List<ProductoDto>> BuscarPorNombreAsync(string nombre);
        Task<List<ProductoDto>> SearchProductosAsync(string searchTerm);
    }
}
