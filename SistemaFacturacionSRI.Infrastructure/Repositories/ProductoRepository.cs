using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de Productos.
    /// </summary>
    public class ProductoRepository : RepositoryBase<Producto>, IProductoRepository
    {
        public ProductoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Producto?> ObtenerPorCodigoAsync(string codigo)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(p => p.Codigo == codigo && p.Activo);
        }

        public async Task<IEnumerable<Producto>> ObtenerProductosConStockAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .Where(p => p.Activo && p.Lotes.Any(l => l.CantidadDisponible > 0))
                .ToListAsync();
        }

        public async Task<IEnumerable<Producto>> BuscarPorNombreAsync(string nombre)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .Where(p => p.Nombre.Contains(nombre) && p.Activo)
                .ToListAsync();
        }

        public async Task<IEnumerable<Producto>> SearchByCodeOrNameAsync(string term)
        {
            var searchTerm = term.ToLower();
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Where(p => p.Activo && (p.Codigo.ToLower().Contains(searchTerm) || p.Nombre.ToLower().Contains(searchTerm)))
                .ToListAsync();
        }

        public override async Task<IEnumerable<Producto>> ObtenerTodosAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .Where(p => p.Activo)
                .ToListAsync();
        }

        public override async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(e => e.Id == id && e.Activo);
        }

        /// <summary>
        /// Obtiene un producto por Id incluyendo también los inactivos.
        /// Útil para reactivación.
        /// </summary>
        public async Task<Producto?> ObtenerPorIdIncluyendoInactivosAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// Obtiene todos los productos incluyendo también los inactivos.
        /// </summary>
        public async Task<IEnumerable<Producto>> ObtenerTodosIncluyendoInactivosAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Include(p => p.Lotes)
                .ToListAsync();
        }
    }
}