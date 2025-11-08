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
    }
}
