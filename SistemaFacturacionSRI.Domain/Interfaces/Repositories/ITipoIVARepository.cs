using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories
{
    public interface ITipoIVARepository
    {
        Task<List<TipoIVACatalogo>> ObtenerTodosAsync();
    }
}
