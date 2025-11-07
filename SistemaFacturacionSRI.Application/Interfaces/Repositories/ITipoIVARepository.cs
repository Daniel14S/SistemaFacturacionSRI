using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    public interface ITipoIVARepository
    {
        Task<List<TipoIVACatalogo>> ObtenerTodosAsync();
    }
}
