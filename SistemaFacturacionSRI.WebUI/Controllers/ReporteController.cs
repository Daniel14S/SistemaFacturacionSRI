using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Infrastructure.Services;

namespace SistemaFacturacionSRI.WebUI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReporteController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly PdfReporteService _pdfService;

    public ReporteController(IReporteService reporteService, PdfReporteService pdfService)
    {
        _reporteService = reporteService;
        _pdfService = pdfService;
    }

    [HttpGet("productos-mas-vendidos")]
    public async Task<IActionResult> ObtenerProductosMasVendidos(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int top = 10)
    {
        var datos = await _reporteService.ObtenerProductosMasVendidosAsync(fechaInicio, fechaFin, top);
        return Ok(datos);
    }

    [HttpGet("productos-mas-vendidos/pdf")]
    public async Task<IActionResult> DescargarPdfProductosMasVendidos(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin)
    {
        var datos = await _reporteService.ObtenerProductosMasVendidosAsync(fechaInicio, fechaFin);
        var pdf = _pdfService.GenerarPdfProductosMasVendidos(datos, fechaInicio, fechaFin);

        return File(pdf, "application/pdf", $"productos-mas-vendidos-{DateTime.Now:yyyyMMdd}.pdf");
    }

    [HttpGet("ventas-por-mes")]
    public async Task<IActionResult> ObtenerVentasPorMes([FromQuery] int año = 0)
    {
        if (año == 0) año = DateTime.Now.Year;
        
        var datos = await _reporteService.ObtenerVentasPorMesAsync(año);
        return Ok(datos);
    }

    [HttpGet("ventas-por-mes/pdf")]
    public async Task<IActionResult> DescargarPdfVentasPorMes([FromQuery] int año = 0)
    {
        if (año == 0) año = DateTime.Now.Year;
        
        var datos = await _reporteService.ObtenerVentasPorMesAsync(año);
        var pdf = _pdfService.GenerarPdfVentasPorMes(datos, año);

        return File(pdf, "application/pdf", $"ventas-por-mes-{año}.pdf");
    }

    [HttpGet("mejor-mes")]
    public async Task<IActionResult> ObtenerMejorMes([FromQuery] int año = 0)
    {
        if (año == 0) año = DateTime.Now.Year;
        
        var datos = await _reporteService.ObtenerMejorMesAsync(año);
        return Ok(datos);
    }
}