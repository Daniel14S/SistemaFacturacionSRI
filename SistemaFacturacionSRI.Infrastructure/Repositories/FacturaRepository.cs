using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Facturas
/// </summary>
public class FacturaRepository : IFacturaRepository
{
    private readonly ApplicationDbContext _context;

    public FacturaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Factura> CrearAsync(Factura factura)
    {
        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();
        return factura;
    }

    public async Task<Factura?> ObtenerPorIdAsync(int id)
    {
        return await _context.Facturas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Factura?> ObtenerPorClaveAccesoAsync(string claveAcceso)
    {
        return await _context.Facturas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ClaveAcceso == claveAcceso);
    }

    public async Task<Factura?> ObtenerConDetallesCompletosAsync(int id)
    {
        return await _context.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c!.TipoIdentificacion)
            .Include(f => f.Detalles)
            .Include(f => f.InfoAdicional)
            .Include(f => f.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Factura?> ObtenerConDetallesPorClaveAccesoAsync(string claveAcceso)
    {
        return await _context.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c!.TipoIdentificacion)
            .Include(f => f.Detalles)
            .Include(f => f.InfoAdicional)
            .Include(f => f.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ClaveAcceso == claveAcceso);
    }

    public async Task<(List<Factura> facturas, int total)> ListarAsync(int pagina, int tamanoPagina)
    {
        var query = _context.Facturas.AsNoTracking();

        var total = await query.CountAsync();
        
        var facturas = await query
            .Include(f => f.Cliente)
            .OrderByDescending(f => f.FechaEmision)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (facturas, total);
    }

    public async Task<(List<Factura> facturas, int total)> ListarConFiltrosAsync(
        int? clienteId = null,
        int? usuarioId = null,
        EstadoFactura? estado = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? numeroFactura = null,
        int pagina = 1,
        int tamanoPagina = 10)
    {
        var query = _context.Facturas.AsNoTracking();

        if (clienteId.HasValue)
            query = query.Where(f => f.ClienteId == clienteId.Value);

        if (usuarioId.HasValue)
            query = query.Where(f => f.UsuarioId == usuarioId.Value);

        if (estado.HasValue)
            query = query.Where(f => f.Estado == estado.Value);

        if (fechaDesde.HasValue)
            query = query.Where(f => f.FechaEmision >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(f => f.FechaEmision <= fechaHasta.Value);

        if (!string.IsNullOrWhiteSpace(numeroFactura))
            query = query.Where(f => f.NumeroFactura.Contains(numeroFactura));

        var total = await query.CountAsync();

        var facturas = await query
            .Include(f => f.Cliente)
            .OrderByDescending(f => f.FechaEmision)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (facturas, total);
    }

    public async Task<List<Factura>> ObtenerPorClienteAsync(int clienteId)
    {
        return await _context.Facturas
            .Where(f => f.ClienteId == clienteId)
            .OrderByDescending(f => f.FechaEmision)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Factura>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        return await _context.Facturas
            .Where(f => f.UsuarioId == usuarioId)
            .OrderByDescending(f => f.FechaEmision)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Factura>> ObtenerPorEstadoAsync(EstadoFactura estado)
    {
        return await _context.Facturas
            .Where(f => f.Estado == estado)
            .OrderByDescending(f => f.FechaEmision)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Factura>> ObtenerPorRangoFechasAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        return await _context.Facturas
            .Where(f => f.FechaEmision >= fechaDesde && f.FechaEmision <= fechaHasta)
            .OrderByDescending(f => f.FechaEmision)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task ActualizarAsync(Factura factura)
    {
        _context.Facturas.Update(factura);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarEstadoAsync(int id, EstadoFactura nuevoEstado)
    {
        var factura = await _context.Facturas.FindAsync(id);
        if (factura != null)
        {
            factura.Estado = nuevoEstado;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ActualizarRespuestaSRIAsync(
        int id,
        string? numeroAutorizacion,
        DateTime? fechaAutorizacion,
        string? xmlPath,
        string? xmlFirmadoPath,
        string? pdfPath,
        string? mensajesSRI)
    {
        var factura = await _context.Facturas.FindAsync(id);
        if (factura != null)
        {
            if (numeroAutorizacion != null)
                factura.NumeroAutorizacion = numeroAutorizacion;
            
            if (fechaAutorizacion.HasValue)
                factura.FechaHoraAutorizacion = fechaAutorizacion;
            
            if (xmlPath != null)
                factura.XmlPath = xmlPath;
            
            if (xmlFirmadoPath != null)
                factura.XmlFirmadoPath = xmlFirmadoPath;
            
            if (pdfPath != null)
                factura.PdfPath = pdfPath;
            
            if (mensajesSRI != null)
                factura.MensajesSRI = mensajesSRI;
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task EliminarAsync(int id)
    {
        var factura = await _context.Facturas.FindAsync(id);
        if (factura != null)
        {
            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExisteNumeroFacturaAsync(string numeroFactura)
    {
        return await _context.Facturas.AnyAsync(f => f.NumeroFactura == numeroFactura);
    }

    public async Task<bool> ExisteClaveAccesoAsync(string claveAcceso)
    {
        return await _context.Facturas.AnyAsync(f => f.ClaveAcceso == claveAcceso);
    }

    public async Task<int> ContarPorEstadoAsync(EstadoFactura estado)
    {
        return await _context.Facturas.CountAsync(f => f.Estado == estado);
    }

    public async Task<decimal> ObtenerTotalFacturadoAsync(DateTime fechaDesde, DateTime fechaHasta)
    {
        return await _context.Facturas
            .Where(f => f.FechaEmision >= fechaDesde 
                     && f.FechaEmision <= fechaHasta 
                     && f.Estado == EstadoFactura.AUTORIZADA)
            .SumAsync(f => f.ImporteTotal);
    }

    public async Task<List<Factura>> ObtenerUltimasAsync(int cantidad)
    {
        return await _context.Facturas
            .Include(f => f.Cliente)
            .OrderByDescending(f => f.FechaEmision)
            .Take(cantidad)
            .AsNoTracking()
            .ToListAsync();
    }
}
