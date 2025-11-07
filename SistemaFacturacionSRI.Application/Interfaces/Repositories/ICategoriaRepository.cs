using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    public interface ICategoriaRepository
    {
        Task<List<Categoria>> ObtenerTodasAsync();
    }
}
