using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Domain.Interfaces.Repositories;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio para ConfiguracionEmpresa
/// </summary>
public class ConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionEmpresaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracionEmpresa?> ObtenerConfiguracionAsync()
    {
        // Retorna el primer registro (debería haber solo uno)
        return await _context.ConfiguracionEmpresa
            .FirstOrDefaultAsync();
    }

    public async Task<ConfiguracionEmpresa?> ObtenerPorIdAsync(int id)
    {
        return await _context.ConfiguracionEmpresa
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ConfiguracionEmpresa> CrearAsync(ConfiguracionEmpresa configuracion)
    {
        _context.ConfiguracionEmpresa.Add(configuracion);
        await _context.SaveChangesAsync();
        return configuracion;
    }

    public async Task ActualizarAsync(ConfiguracionEmpresa configuracion)
    {
        _context.ConfiguracionEmpresa.Update(configuracion);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteConfiguracionAsync()
    {
        return await _context.ConfiguracionEmpresa.AnyAsync();
    }
}
