using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.DTOs.Common;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Consultas de facturas con filtros y relaciones cargadas.
    /// </summary>
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;
        private static readonly IReadOnlyDictionary<EstadoFactura, EstadoFactura[]> _transicionesPermitidas =
            new Dictionary<EstadoFactura, EstadoFactura[]>
            {
                [EstadoFactura.BORRADOR] = new[] { EstadoFactura.GENERADA, EstadoFactura.ANULADA },
                [EstadoFactura.GENERADA] = new[] { EstadoFactura.FIRMADA, EstadoFactura.ANULADA },
                [EstadoFactura.FIRMADA] = new[] { EstadoFactura.ENVIADA, EstadoFactura.ANULADA },
                [EstadoFactura.ENVIADA] = new[] { EstadoFactura.RECIBIDA, EstadoFactura.DEVUELTA, EstadoFactura.ANULADA },
                [EstadoFactura.RECIBIDA] = new[] { EstadoFactura.AUTORIZADA, EstadoFactura.NO_AUTORIZADA, EstadoFactura.DEVUELTA },
                [EstadoFactura.DEVUELTA] = new[] { EstadoFactura.ENVIADA, EstadoFactura.ANULADA },
                [EstadoFactura.NO_AUTORIZADA] = new[] { EstadoFactura.GENERADA, EstadoFactura.ANULADA },
                [EstadoFactura.AUTORIZADA] = Array.Empty<EstadoFactura>(),
                [EstadoFactura.ANULADA] = Array.Empty<EstadoFactura>()
            };
        private const string RolAdministrador = "Administrador";

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

        public async Task<Factura> ActualizarEstadoAsync(int facturaId, EstadoFactura nuevoEstado, CancellationToken cancellationToken = default)
        {
            var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);
            if (factura == null)
            {
                throw new KeyNotFoundException($"No existe una factura con Id {facturaId}.");
            }

            if (factura.Estado == nuevoEstado)
            {
                throw new InvalidOperationException("La factura ya se encuentra en el estado solicitado.");
            }

            if (!TransicionPermitida(factura.Estado, nuevoEstado))
            {
                throw new InvalidOperationException($"No es posible cambiar la factura {factura.NumeroFactura} de {factura.Estado} a {nuevoEstado}.");
            }

            factura.Estado = nuevoEstado;
            factura.FechaModificacion = DateTime.UtcNow;

            if (nuevoEstado == EstadoFactura.AUTORIZADA)
            {
                var fechaAutorizacion = DateTime.UtcNow;
                factura.FechaAutorizacion = fechaAutorizacion;
                factura.FechaHoraAutorizacion = fechaAutorizacion;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return factura;
        }

        public async Task<Factura> AnularFacturaAsync(int facturaId, int usuarioId, string? motivo = null, CancellationToken cancellationToken = default)
        {
            var factura = await _context.Facturas.FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);
            if (factura == null)
            {
                throw new KeyNotFoundException($"No existe una factura con Id {facturaId}.");
            }

            if (factura.Estado != EstadoFactura.AUTORIZADA)
            {
                throw new InvalidOperationException("Solo se pueden anular facturas en estado AUTORIZADA.");
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);

            if (usuario == null)
            {
                throw new KeyNotFoundException($"No existe un usuario con Id {usuarioId}.");
            }

            if (!string.Equals(usuario.Rol?.NombreRol, RolAdministrador, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Solo un administrador puede anular facturas autorizadas.");
            }

            factura.Estado = EstadoFactura.ANULADA;
            factura.FechaModificacion = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(motivo))
            {
                factura.Observaciones = RegistrarMotivoAnulacion(factura.Observaciones, motivo);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return factura;
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

        private static bool TransicionPermitida(EstadoFactura estadoActual, EstadoFactura nuevoEstado)
        {
            return _transicionesPermitidas.TryGetValue(estadoActual, out var permitidos) && permitidos.Contains(nuevoEstado);
        }

        private static string RegistrarMotivoAnulacion(string? observacionesActuales, string motivo)
        {
            var prefijo = $"ANULADA ({DateTime.UtcNow:yyyy-MM-dd HH:mm}): ";
            var nuevoTexto = prefijo + motivo.Trim();

            if (string.IsNullOrWhiteSpace(observacionesActuales))
            {
                return nuevoTexto;
            }

            return string.Join(Environment.NewLine, observacionesActuales.Trim(), nuevoTexto);
        }
    }
}
