using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Gestiona la configuración empresarial para la emisión electrónica.
    /// </summary>
    public class ConfiguracionService : IConfiguracionService
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConfiguracionEmpresa>> ObtenerTodasAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ConfiguracionesEmpresa
                .AsNoTracking()
                .OrderBy(c => c.Establecimiento)
                .ThenBy(c => c.PuntoEmision)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConfiguracionEmpresa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ConfiguracionesEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<ConfiguracionEmpresa?> ObtenerPorEstablecimientoAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default)
        {
            establecimiento = NormalizarCodigo(establecimiento);
            puntoEmision = NormalizarCodigo(puntoEmision);

            return await _context.ConfiguracionesEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Establecimiento == establecimiento && c.PuntoEmision == puntoEmision, cancellationToken);
        }

        public async Task<ConfiguracionEmpresa> CrearAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default)
        {
            if (configuracion == null) throw new ArgumentNullException(nameof(configuracion));

            Normalizar(configuracion);
            await ValidarUnicidadAsync(configuracion, cancellationToken);

            await _context.ConfiguracionesEmpresa.AddAsync(configuracion, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return configuracion;
        }

        public async Task<ConfiguracionEmpresa> ActualizarAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken = default)
        {
            if (configuracion == null) throw new ArgumentNullException(nameof(configuracion));

            var existente = await _context.ConfiguracionesEmpresa.FirstOrDefaultAsync(c => c.Id == configuracion.Id, cancellationToken);
            if (existente is null)
            {
                throw new KeyNotFoundException($"No existe una configuración con Id {configuracion.Id}");
            }

            Normalizar(configuracion);
            await ValidarUnicidadAsync(configuracion, cancellationToken, configuracion.Id);

            existente.Ruc = configuracion.Ruc;
            existente.RazonSocial = configuracion.RazonSocial;
            existente.NombreComercial = configuracion.NombreComercial;
            existente.DirMatriz = configuracion.DirMatriz;
            existente.DirEstablecimiento = configuracion.DirEstablecimiento;
            existente.ContribuyenteEspecial = configuracion.ContribuyenteEspecial;
            existente.ObligadoContabilidad = configuracion.ObligadoContabilidad;
            existente.Establecimiento = configuracion.Establecimiento;
            existente.PuntoEmision = configuracion.PuntoEmision;
            existente.AmbienteSRI = configuracion.AmbienteSRI;
            existente.TipoEmision = configuracion.TipoEmision;
            existente.RutaCertificado = configuracion.RutaCertificado;
            existente.ClaveCertificado = configuracion.ClaveCertificado;
            existente.Logo = configuracion.Logo;

            await _context.SaveChangesAsync(cancellationToken);
            return existente;
        }

        public async Task EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            var configuracion = await _context.ConfiguracionesEmpresa.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (configuracion == null)
            {
                return;
            }

            _context.ConfiguracionesEmpresa.Remove(configuracion);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidarUnicidadAsync(ConfiguracionEmpresa configuracion, CancellationToken cancellationToken, int? excluirId = null)
        {
            var rucDuplicado = await _context.ConfiguracionesEmpresa
                .AnyAsync(c => c.Ruc == configuracion.Ruc && (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (rucDuplicado)
            {
                throw new InvalidOperationException($"Ya existe una configuración registrada con el RUC {configuracion.Ruc}");
            }

            var combinacionDuplicada = await _context.ConfiguracionesEmpresa
                .AnyAsync(c => c.Establecimiento == configuracion.Establecimiento && c.PuntoEmision == configuracion.PuntoEmision && (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (combinacionDuplicada)
            {
                throw new InvalidOperationException($"Ya existe una configuración para el establecimiento {configuracion.Establecimiento}-{configuracion.PuntoEmision}");
            }
        }

        private static void Normalizar(ConfiguracionEmpresa configuracion)
        {
            configuracion.Ruc = configuracion.Ruc.Trim();
            configuracion.RazonSocial = configuracion.RazonSocial.Trim();
            configuracion.NombreComercial = configuracion.NombreComercial?.Trim();
            configuracion.DirMatriz = configuracion.DirMatriz.Trim();
            configuracion.DirEstablecimiento = configuracion.DirEstablecimiento.Trim();
            configuracion.ContribuyenteEspecial = configuracion.ContribuyenteEspecial?.Trim();
            configuracion.Establecimiento = NormalizarCodigo(configuracion.Establecimiento);
            configuracion.PuntoEmision = NormalizarCodigo(configuracion.PuntoEmision);
            configuracion.RutaCertificado = configuracion.RutaCertificado.Trim();
            configuracion.ClaveCertificado = configuracion.ClaveCertificado.Trim();
        }

        private static string NormalizarCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código es obligatorio", nameof(codigo));
            }

            return codigo.Trim().PadLeft(3, '0');
        }
    }
}
