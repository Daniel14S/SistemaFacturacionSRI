using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.WebUI.Services
{
    public interface ICategoriaHttpService
    {
        Task<List<CategoriaDto>> ObtenerTodasAsync();
        Task<List<CategoriaDto>> ObtenerActivasAsync();
        Task<CategoriaDto?> ObtenerPorIdAsync(int id);
        Task<CategoriaDto?> ObtenerPorCodigoAsync(string codigo);
        Task<List<CategoriaDto>> BuscarAsync(string termino);
        Task<CategoriaDto> CrearAsync(CreateCategoriaDto dto);
        Task<CategoriaDto> ActualizarAsync(UpdateCategoriaDto dto);
        Task<bool> EliminarAsync(int id);
        Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null);
    }
}