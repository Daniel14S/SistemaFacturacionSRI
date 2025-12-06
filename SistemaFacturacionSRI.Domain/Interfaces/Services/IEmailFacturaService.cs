using System.Threading;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services;

/// <summary>
/// Servicio para enviar comprobantes de factura por correo.
/// </summary>
public interface IEmailFacturaService
{
    Task EnviarFacturaAsync(int facturaId, CancellationToken cancellationToken = default);
}
