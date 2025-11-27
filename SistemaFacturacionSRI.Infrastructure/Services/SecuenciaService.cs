using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Services;
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

        public SecuenciaService(ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}
