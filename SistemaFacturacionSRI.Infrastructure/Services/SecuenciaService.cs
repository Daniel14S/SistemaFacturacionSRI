using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Implementación thread-safe para la generación de secuencias de factura.
    /// </summary>
    public class SecuenciaService : ISecuenciaService
    {
        private readonly ApplicationDbContext _context;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        
        // Configuración por defecto - puede ser inyectada desde configuración
        private const string ESTABLECIMIENTO_DEFAULT = "001";
        private const string PUNTO_EMISION_DEFAULT = "001";

        public SecuenciaService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Métodos de la Interfaz ISecuenciaService

        public async Task<string> ObtenerSiguienteNumeroAsync()
        {
            var secuenciaActual = await ObtenerSecuenciaActualAsync(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT);
            var siguienteNumero = secuenciaActual + 1;
            return siguienteNumero.ToString("D9");
        }

        public async Task<bool> ActualizarSecuenciaAsync()
        {
            try
            {
                await GenerarSiguienteNumeroFacturaAsync(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GenerarNumeroCompletoAsync()
        {
            return await GenerarSiguienteNumeroFacturaAsync(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT);
        }

        public async Task<string> ObtenerSecuencialActualAsync()
        {
            var secuenciaActual = await ObtenerSecuenciaActualAsync(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT);
            return secuenciaActual.ToString("D9");
        }

        public async Task<bool> ResetearSecuenciaAsync(string nuevoSecuencial)
        {
            if (string.IsNullOrWhiteSpace(nuevoSecuencial) || !long.TryParse(nuevoSecuencial, out var numero))
            {
                throw new ArgumentException("El secuencial debe ser un número válido", nameof(nuevoSecuencial));
            }

            var key = ObtenerKey(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT);
            var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync();
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var secuencia = await _context.SecuenciasFactura
                    .FirstOrDefaultAsync(s => s.Establecimiento == ESTABLECIMIENTO_DEFAULT && 
                                            s.PuntoEmision == PUNTO_EMISION_DEFAULT);

                if (secuencia is null)
                {
                    secuencia = new SecuenciaFactura
                    {
                        Establecimiento = ESTABLECIMIENTO_DEFAULT,
                        PuntoEmision = PUNTO_EMISION_DEFAULT,
                        SecuenciaActual = numero,
                        Activo = true,
                        FechaUltimaEmision = DateTime.UtcNow
                    };

                    await _context.SecuenciasFactura.AddAsync(secuencia);
                }
                else
                {
                    secuencia.SecuenciaActual = numero;
                    secuencia.FechaUltimaEmision = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<SecuenciaInfo> ObtenerInfoSecuenciaAsync()
        {
            var secuencia = await _context.SecuenciasFactura
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Establecimiento == ESTABLECIMIENTO_DEFAULT && 
                                        s.PuntoEmision == PUNTO_EMISION_DEFAULT);

            if (secuencia is null)
            {
                return new SecuenciaInfo
                {
                    Establecimiento = ESTABLECIMIENTO_DEFAULT,
                    PuntoEmision = PUNTO_EMISION_DEFAULT,
                    SecuencialActual = "000000000",
                    SiguienteSecuencial = "000000001",
                    NumeroCompleto = FormatearNumero(ESTABLECIMIENTO_DEFAULT, PUNTO_EMISION_DEFAULT, 1),
                    UltimaActualizacion = null
                };
            }

            var siguiente = secuencia.SecuenciaActual + 1;

            return new SecuenciaInfo
            {
                Establecimiento = secuencia.Establecimiento,
                PuntoEmision = secuencia.PuntoEmision,
                SecuencialActual = secuencia.SecuenciaActual.ToString("D9"),
                SiguienteSecuencial = siguiente.ToString("D9"),
                NumeroCompleto = FormatearNumero(secuencia.Establecimiento, secuencia.PuntoEmision, siguiente),
                UltimaActualizacion = secuencia.FechaUltimaEmision
            };
        }

        #endregion

        #region Métodos Auxiliares Originales

        public async Task<string> GenerarSiguienteNumeroFacturaAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default)
        {
            ValidarCodigo(establecimiento, nameof(establecimiento));
            ValidarCodigo(puntoEmision, nameof(puntoEmision));

            var key = ObtenerKey(establecimiento, puntoEmision);
            var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync(cancellationToken);
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

                var secuencia = await _context.SecuenciasFactura
                    .FirstOrDefaultAsync(s => s.Establecimiento == establecimiento && s.PuntoEmision == puntoEmision, cancellationToken);

                if (secuencia is null)
                {
                    secuencia = new SecuenciaFactura
                    {
                        Establecimiento = establecimiento,
                        PuntoEmision = puntoEmision,
                        SecuenciaActual = 0,
                        Activo = true
                    };

                    await _context.SecuenciasFactura.AddAsync(secuencia, cancellationToken);
                }

                secuencia.SecuenciaActual++;
                secuencia.FechaUltimaEmision = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return FormatearNumero(secuencia.Establecimiento, secuencia.PuntoEmision, secuencia.SecuenciaActual);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<long> ObtenerSecuenciaActualAsync(string establecimiento, string puntoEmision, CancellationToken cancellationToken = default)
        {
            ValidarCodigo(establecimiento, nameof(establecimiento));
            ValidarCodigo(puntoEmision, nameof(puntoEmision));

            var secuencia = await _context.SecuenciasFactura
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Establecimiento == establecimiento && s.PuntoEmision == puntoEmision, cancellationToken);

            return secuencia?.SecuenciaActual ?? 0;
        }

        private static string ObtenerKey(string establecimiento, string punto) => $"{establecimiento}-{punto}";

        private static void ValidarCodigo(string value, string nombre)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("El valor es obligatorio", nombre);
            }

            if (value.Trim().Length != 3)
            {
                throw new ArgumentException("El código debe tener exactamente 3 caracteres", nombre);
            }
        }

        private static string FormatearNumero(string establecimiento, string puntoEmision, long secuencia)
        {
            var estab = establecimiento.Trim().PadLeft(3, '0');
            var punto = puntoEmision.Trim().PadLeft(3, '0');
            var consecutivo = secuencia.ToString("D9");
            return $"{estab}-{punto}-{consecutivo}";
        }

        #endregion
    }
}