using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Maneja la configuración empresarial requerida para la emisión electrónica.
    /// </summary>
    public interface IConfiguracionService
    {
        Task<IEnumerable<ConfiguracionEmpresa>> ObtenerTodasAsync(CancellationToken cancellationToken = default);
        Task<ConfiguracionEmpresa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ConfiguracionEmpresa?> ObtenerPorEstablecimientoAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default);
        Task<ConfiguracionEmpresa> CrearAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default);
        Task<ConfiguracionEmpresa> ActualizarAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default);
        Task EliminarAsync(int id, CancellationToken cancellationToken = default);
    }
}
