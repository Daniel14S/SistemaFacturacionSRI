using SistemaFacturacionSRI.Domain.Entities;

namespace SistemaFacturacionSRI.Application.Interfaces.Repositories
{
    /// <summary>
    /// Interfaz específica para el repositorio de Productos.
    /// Extiende el repositorio base y agrega métodos específicos de Producto.
    /// </summary>
    public interface IProductoRepository : IRepositoryBase<Producto>
    {
        /// <summary>
        /// Busca un producto por su código único.
        /// </summary>
        Task<Producto?> ObtenerPorCodigoAsync(string codigo);

        /// <summary>
        /// Obtiene productos que tienen stock disponible.
        /// </summary>
        Task<IEnumerable<Producto>> ObtenerProductosConStockAsync();

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        Task<IEnumerable<Producto>> BuscarPorNombreAsync(string nombre);

        /// <summary>
        /// Busca productos por código o nombre.
        /// </summary>
        Task<IEnumerable<Producto>> SearchByCodeOrNameAsync(string term);

        /// <summary>
        /// Obtiene un producto por Id incluyendo también los inactivos.
        /// </summary>
        Task<Producto?> ObtenerPorIdIncluyendoInactivosAsync(int id);

        /// <summary>
        /// Obtiene todos los productos incluyendo también los inactivos.
        /// Útil para vistas que necesitan filtrar por Activo/Inactivo en la capa superior.
        /// </summary>
        Task<IEnumerable<Producto>> ObtenerTodosIncluyendoInactivosAsync();
    }
}