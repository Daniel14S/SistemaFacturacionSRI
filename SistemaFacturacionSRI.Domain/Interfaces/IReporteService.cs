using SistemaFacturacionSRI.Domain.DTOs.Reportes;

namespace SistemaFacturacionSRI.Domain.Interfaces;

public interface IReporteService
{
    Task<List<ReporteProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int top = 10);
    Task<List<ReporteVentasMesDto>> ObtenerVentasPorMesAsync(int año);
    Task<ReporteMejorMesDto?> ObtenerMejorMesAsync(int año);
}