using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    public interface ICategoriaService
    {
        Task<IEnumerable<CategoriaDto>> ObtenerTodasAsync();
        Task<IEnumerable<CategoriaDto>> ObtenerActivasAsync();
        Task<CategoriaDto?> ObtenerPorIdAsync(int id);
        Task<CategoriaDto?> ObtenerPorCodigoAsync(string codigo);
        Task<CategoriaDto> CrearAsync(CreateCategoriaDto dto);
        Task<CategoriaDto?> ActualizarAsync(UpdateCategoriaDto dto);
        Task<bool> EliminarAsync(int id);
        Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null);
        Task<IEnumerable<CategoriaDto>> BuscarAsync(string termino);
    }
}
