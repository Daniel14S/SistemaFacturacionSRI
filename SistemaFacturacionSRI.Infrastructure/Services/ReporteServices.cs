using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Domain.DTOs.Reportes;
using SistemaFacturacionSRI.Domain.Interfaces;
using SistemaFacturacionSRI.Domain.Enums;
using System.Globalization;

namespace SistemaFacturacionSRI.Infrastructure.Services;

public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReporteProductoMasVendidoDto>> ObtenerProductosMasVendidosAsync(
        DateTime? fechaInicio = null, 
        DateTime? fechaFin = null, 
        int top = 10)
    {
        var query = _context.DetalleFacturas
            .Include(fd => fd.Producto)
            .Include(fd => fd.Factura)
            .Where(fd => fd.Factura!.Estado == EstadoFactura.AUTORIZADA);

        if (fechaInicio.HasValue)
            query = query.Where(fd => fd.Factura!.FechaEmision >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(fd => fd.Factura!.FechaEmision <= fechaFin.Value);

        var resultado = await query
            .GroupBy(fd => new
            {
                fd.Producto!.Id,
                fd.Producto.Codigo,
                fd.Producto.Nombre
            })
            .Select(g => new ReporteProductoMasVendidoDto
            {
                Codigo = g.Key.Codigo,
                Nombre = g.Key.Nombre,
                CantidadVendida = g.Sum(fd => fd.Cantidad),
                MontoTotal = g.Sum(fd => fd.ValorTotal),
                NumeroFacturas = g.Select(fd => fd.FacturaId).Distinct().Count()
            })
            .OrderByDescending(r => r.CantidadVendida)
            .Take(top)
            .ToListAsync();

        return resultado;
    }

    public async Task<List<ReporteVentasMesDto>> ObtenerVentasPorMesAsync(int año)
    {
        var culture = new CultureInfo("es-ES");
        
        var resultado = await _context.Facturas
            .Where(f => f.Estado == EstadoFactura.AUTORIZADA && 
                       f.FechaEmision.Year == año)
            .GroupBy(f => new
            {
                Año = f.FechaEmision.Year,
                Mes = f.FechaEmision.Month
            })
            .Select(g => new ReporteVentasMesDto
            {
                Año = g.Key.Año,
                Mes = g.Key.Mes,
                NumeroFacturas = g.Count(),
                TotalFacturado = g.Sum(f => f.ImporteTotal),
                IVARecaudado = g.Sum(f => f.IVA15)
            })
            .OrderBy(r => r.Mes)
            .ToListAsync();

        foreach (var item in resultado)
        {
            item.NombreMes = culture.DateTimeFormat.GetMonthName(item.Mes);
        }

        return resultado;
    }

    public async Task<ReporteMejorMesDto?> ObtenerMejorMesAsync(int año)
    {
        var culture = new CultureInfo("es-ES");
        
        var resultado = await _context.Facturas
            .Where(f => f.Estado == EstadoFactura.AUTORIZADA && 
                       f.FechaEmision.Year == año)
            .GroupBy(f => new
            {
                Año = f.FechaEmision.Year,
                Mes = f.FechaEmision.Month
            })
            .Select(g => new ReporteMejorMesDto
            {
                Año = g.Key.Año,
                Mes = g.Key.Mes,
                TotalVentas = g.Sum(f => f.ImporteTotal),
                NumeroFacturas = g.Count(),
                PromedioFactura = g.Average(f => f.ImporteTotal)
            })
            .OrderByDescending(r => r.TotalVentas)
            .FirstOrDefaultAsync();

        if (resultado != null)
        {
            resultado.NombreMes = culture.DateTimeFormat.GetMonthName(resultado.Mes);
        }

        return resultado;
    }
}