using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Domain.Interfaces.Repositories;

public interface ICertificadoDigitalRepository
{
    Task<CertificadoDigital?> ObtenerActivoAsync(CancellationToken cancellationToken = default);
    Task<CertificadoDigital?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task CrearAsync(CertificadoDigital certificado, CancellationToken cancellationToken = default);
    Task DesactivarTodosAsync(CancellationToken cancellationToken = default);
    Task MarcarInactivoAsync(int id, CancellationToken cancellationToken = default);
    Task EliminarTodosAsync(CancellationToken cancellationToken = default);
}
