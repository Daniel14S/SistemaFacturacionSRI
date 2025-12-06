using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories;

public class CertificadoDigitalRepository : ICertificadoDigitalRepository
{
    private readonly ApplicationDbContext _context;

    public CertificadoDigitalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CertificadoDigital?> ObtenerActivoAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificadoDigital>()
            .AsNoTracking()
            .Where(c => c.EsActivo && c.Activo)
            .OrderByDescending(c => c.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CertificadoDigital?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificadoDigital>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task CrearAsync(CertificadoDigital certificado, CancellationToken cancellationToken = default)
    {
        await _context.Set<CertificadoDigital>().AddAsync(certificado, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DesactivarTodosAsync(CancellationToken cancellationToken = default)
    {
        var activos = await _context.Set<CertificadoDigital>()
            .Where(c => c.EsActivo)
            .ToListAsync(cancellationToken);

        foreach (var cert in activos)
        {
            cert.EsActivo = false;
            cert.Activo = false;
            cert.FechaModificacion = DateTime.UtcNow;
        }

        if (activos.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarcarInactivoAsync(int id, CancellationToken cancellationToken = default)
    {
        var certificado = await _context.Set<CertificadoDigital>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (certificado != null)
        {
            certificado.EsActivo = false;
            certificado.Activo = false;
            certificado.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task EliminarTodosAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.Set<CertificadoDigital>().ToListAsync(cancellationToken);
        if (all.Count == 0)
        {
            return;
        }

        _context.Set<CertificadoDigital>().RemoveRange(all);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
