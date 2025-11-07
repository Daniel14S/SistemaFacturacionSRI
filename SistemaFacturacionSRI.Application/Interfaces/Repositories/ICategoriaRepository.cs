using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Interfaz específica para el repositorio de Categorías.
    /// Extiende el repositorio base y agrega métodos específicos de Categoría.
    /// </summary>
    public interface ICategoriaRepository : IRepositoryBase<Categoria>
    {
        /// <summary>
        /// Busca una categoría por su código único.
        /// </summary>
        /// <param name="codigo">Código de la categoría</param>
        /// <returns>Categoría encontrada o null</returns>
        Task<Categoria?> ObtenerPorCodigoAsync(string codigo);

        /// <summary>
        /// Obtiene categorías que tienen al menos un producto asignado.
        /// </summary>
        /// <returns>Lista de categorías con productos</returns>
        Task<IEnumerable<Categoria>> ObtenerCategoriasConProductosAsync();

        /// <summary>
        /// Busca categorías por nombre (búsqueda parcial).
        /// </summary>
        /// <param name="nombre">Texto a buscar</param>
        /// <returns>Lista de categorías que coinciden</returns>
        Task<IEnumerable<Categoria>> BuscarPorNombreAsync(string nombre);

        /// <summary>
        /// Obtiene una categoría con sus productos relacionados.
        /// </summary>
        /// <param name="id">ID de la categoría</param>
        /// <returns>Categoría con productos incluidos</returns>
        Task<Categoria?> ObtenerConProductosAsync(int id);

        Task<IEnumerable<Categoria>> ObtenerTodasAsync();
    }
}
