using SistemaFacturacionSRI.Domain.DTOs.TipoIVA;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    public interface ITipoIVAService
    {
        Task<List<TipoIVADto>> ObtenerTodosAsync();
    }
}
