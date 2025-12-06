using SistemaFacturacionSRI.Domain.DTOs.Configuracion;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

/// <summary>
/// Maneja el almacenamiento seguro del certificado digital (.p12) y su clave dentro de la base de datos.
/// </summary>
public interface ICertificadoDigitalStorageService
{
    Task<CertificadoDigitalActivoDto?> ObtenerCertificadoActivoAsync(CancellationToken cancellationToken = default);
    Task<CertificadoDigitalActivoDto> GuardarCertificadoAsync(GuardarCertificadoRequest request, CancellationToken cancellationToken = default);
    Task<bool> EliminarCertificadoActivoAsync(CancellationToken cancellationToken = default);
}
