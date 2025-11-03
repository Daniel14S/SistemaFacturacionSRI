using System.Linq.Expressions;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Interfaz base para todos los repositorios.
    /// Define operaciones CRUD comunes.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    public interface IRepositoryBase<T> where T : class
    {
        /// <summary>
        /// Obtiene todos los registros de la entidad.
        /// </summary>
        Task<IEnumerable<T>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene un registro por su ID.
        /// </summary>
        Task<T?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Busca registros que cumplan una condición.
        /// </summary>
        Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicado);

        /// <summary>
        /// Agrega una nueva entidad.
        /// </summary>
        Task<T> AgregarAsync(T entidad);

        /// <summary>
        /// Actualiza una entidad existente.
        /// </summary>
        Task ActualizarAsync(T entidad);

        /// <summary>
        /// Elimina una entidad (eliminación lógica o física).
        /// </summary>
        Task EliminarAsync(int id);

        /// <summary>
        /// Verifica si existe un registro que cumple una condición.
        /// </summary>
        Task<bool> ExisteAsync(Expression<Func<T, bool>> predicado);
    }
}