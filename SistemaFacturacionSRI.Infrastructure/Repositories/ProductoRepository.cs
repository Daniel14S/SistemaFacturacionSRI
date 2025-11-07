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
                .FirstOrDefaultAsync(p => p.Codigo == codigo && p.Activo);
        }

        public async Task<IEnumerable<Producto>> ObtenerProductosConStockAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Where(p => p.Stock > 0 && p.Activo)
                .ToListAsync();
        }

        public async Task<IEnumerable<Producto>> BuscarPorNombreAsync(string nombre)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Where(p => p.Nombre.Contains(nombre) && p.Activo)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Producto>> ObtenerTodosAsync()
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .Where(p => p.Activo)
                .ToListAsync();
        }

        public override async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Categoria)
                .Include(p => p.TipoIVACatalogo)
                .FirstOrDefaultAsync(e => e.Id == id && e.Activo);
        }
    }
}