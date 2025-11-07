using SistemaFacturacionSRI.Application.DTOs.TipoIVA;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    public interface ITipoIVAService
    {
        Task<List<TipoIVADto>> ObtenerTodosAsync();
    }
}
