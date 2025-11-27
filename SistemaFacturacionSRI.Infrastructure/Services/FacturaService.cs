using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.DTOs.Common;
using SistemaFacturacionSRI.Application.DTOs.Factura;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Consultas de facturas con filtros y relaciones cargadas.
    /// </summary>
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;

        public FacturaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<Factura>> ListarFacturasAsync(FiltroFacturaDto filtro, CancellationToken cancellationToken = default)
        {
            if (filtro == null)
            {
                throw new ArgumentNullException(nameof(filtro));
            }

            var query = _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Usuario)
                .Include(f => f.Detalles)!.ThenInclude(d => d.Producto)
                .Include(f => f.InformacionAdicional)
                .AsQueryable();

            if (filtro.ClienteId.HasValue)
            {
                query = query.Where(f => f.ClienteId == filtro.ClienteId.Value);
            }

            if (filtro.UsuarioId.HasValue)
            {
                query = query.Where(f => f.UsuarioId == filtro.UsuarioId.Value);
            }

            if (filtro.Estado.HasValue)
            {
                query = query.Where(f => f.Estado == filtro.Estado.Value);
            }

            if (filtro.FechaDesde.HasValue)
            {
                var desde = filtro.FechaDesde.Value.Date;
                query = query.Where(f => f.FechaEmision >= desde);
            }

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(f => f.FechaEmision <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(filtro.NumeroFactura))
            {
                var numero = filtro.NumeroFactura.Trim();
                query = query.Where(f => f.NumeroFactura.Contains(numero));
            }

            if (!string.IsNullOrWhiteSpace(filtro.ClaveAcceso))
            {
                var clave = filtro.ClaveAcceso.Trim();
                query = query.Where(f => f.ClaveAcceso != null && f.ClaveAcceso.Contains(clave));
            }

            if (!string.IsNullOrWhiteSpace(filtro.TextoBusqueda))
            {
                var term = filtro.TextoBusqueda.Trim().ToLower();
                query = query.Where(f =>
                    (f.Cliente != null && (f.Cliente.Nombre1 + " " + f.Cliente.Apellido1).ToLower().Contains(term)) ||
                    (f.Usuario != null && f.Usuario.Username.ToLower().Contains(term)) ||
                    f.NumeroFactura.Contains(term));
            }

            var totalItems = await query.CountAsync(cancellationToken);

            var pageNumber = filtro.PageNumber < 1 ? 1 : filtro.PageNumber;
            var pageSize = filtro.PageSize < 1 ? 10 : filtro.PageSize;

            query = AplicarOrdenamiento(query, filtro.OrderBy, filtro.OrderAscending);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResultDto<Factura>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Factura?> ObtenerPorIdAsync(int facturaId, CancellationToken cancellationToken = default)
        {
            return await _context.Facturas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Usuario)
                .Include(f => f.Detalles)!.ThenInclude(d => d.Producto)
                .Include(f => f.InformacionAdicional)
                .FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);
        }

        private static IQueryable<Factura> AplicarOrdenamiento(IQueryable<Factura> query, string? orderBy, bool ascending)
        {
            return orderBy?.ToLower() switch
            {
                "numero" or "numerofactura" => ascending ? query.OrderBy(f => f.NumeroFactura) : query.OrderByDescending(f => f.NumeroFactura),
                "cliente" => ascending
                    ? query.OrderBy(f => f.Cliente != null ? f.Cliente.Nombre1 : string.Empty)
                    : query.OrderByDescending(f => f.Cliente != null ? f.Cliente.Nombre1 : string.Empty),
                "total" or "importetotal" => ascending ? query.OrderBy(f => f.ImporteTotal) : query.OrderByDescending(f => f.ImporteTotal),
                _ => ascending ? query.OrderBy(f => f.FechaEmision) : query.OrderByDescending(f => f.FechaEmision)
            };
        }
    }
}
