using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de Categorías.
    /// Proporciona acceso a datos específico para categorías.
    /// </summary>
    public class CategoriaRepository : RepositoryBase<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Busca una categoría por su código único.
        /// </summary>
        public async Task<Categoria?> ObtenerPorCodigoAsync(string codigo)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Codigo == codigo && c.Activo);
        }

        /// <summary>
        /// Obtiene categorías que tienen al menos un producto asignado.
        /// Usa Include para traer los productos relacionados.
        /// </summary>
        public async Task<IEnumerable<Categoria>> ObtenerCategoriasConProductosAsync()
        {
            return await _dbSet
                .Include(c => c.Productos)  // Eager loading: trae productos
                .Where(c => c.Activo && c.Productos.Any(p => p.Activo))
                .ToListAsync();
        }

        /// <summary>
        /// Busca categorías por nombre (búsqueda parcial).
        /// Ejemplo: "Elec" encuentra "Electrónica"
        /// </summary>
        public async Task<IEnumerable<Categoria>> BuscarPorNombreAsync(string nombre)
        {
            return await _dbSet
                .Where(c => c.Nombre.Contains(nombre) && c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene una categoría con sus productos relacionados.
        /// Útil para mostrar detalles de categoría con listado de productos.
        /// </summary>
        public async Task<Categoria?> ObtenerConProductosAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Productos.Where(p => p.Activo))  // Solo productos activos
                .FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        }

        public async Task<IEnumerable<Categoria>> ObtenerTodasAsync()
        {
            return await _dbSet
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }
    }
}
