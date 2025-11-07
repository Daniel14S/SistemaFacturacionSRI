using SistemaFacturacionSRI.Application.DTOs.Categoria;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    public interface ICategoriaService
    {
        Task<List<CategoriaDto>> ObtenerTodasAsync();
    }
}
