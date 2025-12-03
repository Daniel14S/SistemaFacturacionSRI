using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.DTOs.Common;

namespace SistemaFacturacionSRI.WebUI.Services;

public interface IFacturaHttpService
{
    Task<PagedResultDto<FacturaDto>> ObtenerFacturasAsync(
        int pageNumber = 1, 
        int pageSize = 10,
        string? numeroFactura = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estado = null);
    
    Task<FacturaDto?> ObtenerPorIdAsync(int id);
    Task<byte[]?> DescargarXmlAsync(int id);
    Task<bool> ReenviarSriAsync(int id);
    Task<bool> AnularAsync(int id, string motivo);
}