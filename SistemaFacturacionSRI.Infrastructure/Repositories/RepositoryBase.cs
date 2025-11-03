using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Application.Interfaces.Repositories;
using SistemaFacturacionSRI.Domain.Entities;
using SistemaFacturacionSRI.Infrastructure.Data;
using System.Linq.Expressions;

namespace SistemaFacturacionSRI.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación base del repositorio genérico.
    /// Proporciona operaciones CRUD comunes para todas las entidades.
    /// </summary>
    public class RepositoryBase<T> : IRepositoryBase<T> where T : EntidadBase
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public RepositoryBase(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> ObtenerTodosAsync()
        {
            return await _dbSet.Where(e => e.Activo).ToListAsync();
        }

        public virtual async Task<T?> ObtenerPorIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.Activo);
        }

        public virtual async Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicado)
        {
            return await _dbSet.Where(predicado).Where(e => e.Activo).ToListAsync();
        }

        public virtual async Task<T> AgregarAsync(T entidad)
        {
            await _dbSet.AddAsync(entidad);
            await _context.SaveChangesAsync();
            return entidad;
        }

        public virtual async Task ActualizarAsync(T entidad)
        {
            _dbSet.Update(entidad);
            await _context.SaveChangesAsync();
        }

        public virtual async Task EliminarAsync(int id)
        {
            var entidad = await ObtenerPorIdAsync(id);
            if (entidad != null)
            {
                // Eliminación lógica
                entidad.Activo = false;
                await ActualizarAsync(entidad);
            }
        }

        public virtual async Task<bool> ExisteAsync(Expression<Func<T, bool>> predicado)
        {
            return await _dbSet.AnyAsync(predicado);
        }
    }
}