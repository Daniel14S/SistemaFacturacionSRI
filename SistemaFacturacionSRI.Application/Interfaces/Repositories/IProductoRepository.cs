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
    }
}