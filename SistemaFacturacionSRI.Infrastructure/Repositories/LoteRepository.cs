using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación de acceso a datos para los lotes.
    /// </summary>
    public class LoteRepository : ILoteRepository
    {
        private readonly ApplicationDbContext _context;

        public LoteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Lote>> ObtenerTodosAsync()
        {
            return await _context.Lotes
                .Include(l => l.Producto)
                    .ThenInclude(p => p!.Categoria)
                .AsNoTracking()
                .OrderByDescending(l => l.FechaCompra)
                .ThenByDescending(l => l.LoteId)
                .ToListAsync();
        }

        public async Task<Lote> CrearAsync(Lote lote)
        {
            await _context.Lotes.AddAsync(lote);
            await _context.SaveChangesAsync();

            // Cargar las relaciones necesarias para el mapeo posterior.
            await _context.Entry(lote).Reference(l => l.Producto).LoadAsync();
            if (lote.Producto != null)
            {
                await _context.Entry(lote.Producto).Reference(p => p.Categoria).LoadAsync();
            }

            return lote;
        }

        public async Task<IEnumerable<Lote>> ObtenerLotesPorProductoAsync(int idProducto)
        {
            return await _context.Lotes
                .Include(l => l.Producto!) // Producto no será null
                    .ThenInclude(p => p.Categoria)
                .Where(l => l.ProductoId == idProducto)
                .AsNoTracking()
                .OrderByDescending(l => l.FechaCompra)
                .ThenByDescending(l => l.LoteId)
                .ToListAsync();
        }


        public async Task<IEnumerable<Lote>> ObtenerPorProductoAsync(int productoId)
        {
            return await _context.Lotes
                .Include(l => l.Producto)
                .Where(l => l.ProductoId == productoId)
                .ToListAsync();
        }

        // SistemaFacturacionSRI.Infrastructure/Repositories/LoteRepository.cs
// Agregar estos métodos a la clase existente:

public async Task<Lote?> ObtenerPorIdAsync(int loteId)
{
    return await _context.Lotes
        .Include(l => l.Producto)
            .ThenInclude(p => p!.Categoria)
        .AsNoTracking()
        .FirstOrDefaultAsync(l => l.LoteId == loteId);
}

public async Task ActualizarAsync(Lote lote)
{
    _context.Lotes.Update(lote);
    await _context.SaveChangesAsync();
}

public async Task EliminarAsync(int loteId)
{
    var lote = await _context.Lotes.FindAsync(loteId);
    if (lote != null)
    {
        _context.Lotes.Remove(lote);
        await _context.SaveChangesAsync();
    }
}


    }
}
