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
                .OrderBy(c => c.CodigoEstablecimiento)
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
                .FirstOrDefaultAsync(c => c.CodigoEstablecimiento == establecimiento && c.PuntoEmision == puntoEmision, cancellationToken);
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

            existente.RUC = configuracion.RUC;
            existente.RazonSocial = configuracion.RazonSocial;
            existente.NombreComercial = configuracion.NombreComercial;
            existente.DirMatriz = configuracion.DirMatriz;
            existente.DirEstablecimiento = configuracion.DirEstablecimiento;
            existente.AgenteRetencion = configuracion.AgenteRetencion;
            existente.ObligadoContabilidad = configuracion.ObligadoContabilidad;
            existente.CodigoEstablecimiento = configuracion.CodigoEstablecimiento;
            existente.PuntoEmision = configuracion.PuntoEmision;
            existente.AmbienteSRI = configuracion.AmbienteSRI;
            existente.TipoEmision = configuracion.TipoEmision;
            existente.RutaCertificadoDigital = configuracion.RutaCertificadoDigital;
            existente.ClaveCertificadoDigital = configuracion.ClaveCertificadoDigital;
            existente.LogoPath = configuracion.LogoPath;

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
                .AnyAsync(c => c.RUC == configuracion.RUC && (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (rucDuplicado)
            {
                throw new InvalidOperationException($"Ya existe una configuración registrada con el RUC {configuracion.RUC}");
            }

            var combinacionDuplicada = await _context.ConfiguracionesEmpresa
                .AnyAsync(c => c.CodigoEstablecimiento == configuracion.CodigoEstablecimiento && c.PuntoEmision == configuracion.PuntoEmision && (!excluirId.HasValue || c.Id != excluirId.Value), cancellationToken);

            if (combinacionDuplicada)
            {
                throw new InvalidOperationException($"Ya existe una configuración para el establecimiento {configuracion.CodigoEstablecimiento}-{configuracion.PuntoEmision}");
            }
        }

        private static void Normalizar(ConfiguracionEmpresa configuracion)
        {
            configuracion.RUC = configuracion.RUC.Trim();
            configuracion.RazonSocial = configuracion.RazonSocial.Trim();
            configuracion.NombreComercial = configuracion.NombreComercial?.Trim() ?? string.Empty;
            configuracion.DirMatriz = configuracion.DirMatriz.Trim();
            configuracion.DirEstablecimiento = configuracion.DirEstablecimiento.Trim();
            configuracion.AgenteRetencion = configuracion.AgenteRetencion?.Trim();
            configuracion.CodigoEstablecimiento = NormalizarCodigo(configuracion.CodigoEstablecimiento);
            configuracion.PuntoEmision = NormalizarCodigo(configuracion.PuntoEmision);
            
            if (!string.IsNullOrWhiteSpace(configuracion.RutaCertificadoDigital))
            {
                configuracion.RutaCertificadoDigital = configuracion.RutaCertificadoDigital.Trim();
            }
            
            if (!string.IsNullOrWhiteSpace(configuracion.ClaveCertificadoDigital))
            {
                configuracion.ClaveCertificadoDigital = configuracion.ClaveCertificadoDigital.Trim();
            }
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