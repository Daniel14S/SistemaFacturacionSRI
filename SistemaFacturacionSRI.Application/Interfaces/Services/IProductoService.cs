using SistemaFacturacionSRI.Application.DTOs.Producto;

namespace SistemaFacturacionSRI.Application.Interfaces.Services
{
    /// <summary>
    /// Interfaz del servicio de Productos.
    /// Define las operaciones de negocio disponibles para productos.
    /// </summary>
    public interface IProductoService
    {
        /// <summary>
        /// Crea un nuevo producto en el sistema.
        /// Valida que el código sea único antes de crear.
        /// </summary>
        /// <param name="dto">Datos del producto a crear</param>
        /// <returns>Producto creado con todos sus datos</returns>
        /// <exception cref="InvalidOperationException">Si el código ya existe</exception>
        Task<ProductoDto> CrearAsync(CrearProductoDto dto);

        /// <summary>
        /// Obtiene todos los productos activos del sistema.
        /// </summary>
        /// <returns>Lista de productos</returns>
        Task<IEnumerable<ProductoDto>> ObtenerTodosAsync();

        /// <summary>
        /// Obtiene un producto específico por su ID.
        /// </summary>
        /// <param name="id">Identificador del producto</param>
        /// <returns>Producto encontrado o null si no existe</returns>
        Task<ProductoDto?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Obtiene un producto por su código único.
        /// </summary>
        /// <param name="codigo">Código del producto</param>
        /// <returns>Producto encontrado o null si no existe</returns>
        Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo);

        /// <summary>
        /// Actualiza los datos de un producto existente.
        /// Valida que el nuevo código sea único (si cambió).
        /// </summary>
        /// <param name="dto">Datos actualizados del producto</param>
        /// <returns>Producto actualizado</returns>
        /// <exception cref="KeyNotFoundException">Si el producto no existe</exception>
        /// <exception cref="InvalidOperationException">Si el nuevo código ya existe</exception>
        Task<ProductoDto> ActualizarAsync(ActualizarProductoDto dto);

        /// <summary>
        /// Elimina lógicamente un producto (marca como inactivo).
        /// </summary>
        /// <param name="id">Identificador del producto a eliminar</param>
        /// <exception cref="KeyNotFoundException">Si el producto no existe</exception>
        Task EliminarAsync(int id);

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        /// <param name="nombre">Texto a buscar en el nombre</param>
        /// <returns>Lista de productos que coinciden</returns>
        Task<IEnumerable<ProductoDto>> BuscarPorNombreAsync(string nombre);

        /// <summary>
        /// Obtiene productos que tienen stock disponible.
        /// </summary>
        /// <returns>Lista de productos con stock > 0</returns>
        Task<IEnumerable<ProductoDto>> ObtenerProductosConStockAsync();

        Task<int> ObtenerCantidadTotalLotesAsync(int idProducto);

        /// <summary>
/// Obtiene todos los productos activos CON su lote prioritario.
/// El lote prioritario es el que tiene la fecha de expiración más cercana.
/// NUEVO: Necesario para mostrar lote próximo a vencer en la interfaz.
/// </summary>
/// <returns>Lista de productos con información del lote más próximo a expirar</returns>
Task<IEnumerable<ProductoDto>> ObtenerTodosConLotePrioritarioAsync();

/// <summary>
/// Busca productos por código o nombre.
/// </summary>
/// <param name="term">Término de búsqueda</param>
/// <returns>Lista de productos que coinciden</returns>
Task<IEnumerable<ProductoDto>> SearchByCodeOrNameAsync(string term);

        /// <summary>
        /// Reactiva un producto previamente inactivado (Activo = true).
        /// </summary>
        /// <param name="id">Identificador del producto a reactivar</param>
        Task ReactivarAsync(int id);
    }
}