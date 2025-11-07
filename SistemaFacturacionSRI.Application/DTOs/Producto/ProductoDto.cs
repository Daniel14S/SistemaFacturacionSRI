using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Application.DTOs.Producto
{
    /// <summary>
    /// DTO para devolver información de un producto.
    /// Se usa en respuestas GET (lectura).
    /// Contiene toda la información que el cliente necesita ver.
    /// </summary>
    public class ProductoDto
    {
        /// <summary>
        /// Identificador único del producto.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Código único del producto (SKU).
        /// </summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del producto.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del producto (puede ser null).
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Precio unitario sin IVA.
        /// </summary>
        public decimal Precio { get; set; }

    /// <summary>
    /// Identificador del tipo de IVA (catálogo en base de datos).
    /// </summary>
    public int TipoIVAId { get; set; }

    /// <summary>
    /// Descripción legible del tipo de IVA (desde el catálogo).
    /// Ejemplo: "IVA 12%"
    /// </summary>
    public string TipoIVADescripcion { get; set; } = string.Empty;

        /// <summary>
        /// Identificador de la categoría a la que pertenece el producto.
        /// </summary>
        public int CategoriaId { get; set; }

        /// <summary>
        /// Nombre de la categoría a la que pertenece el producto.
        /// </summary>
        public string CategoriaNombre { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad en stock.
        /// </summary>
        public int Stock { get; set; }

        

        /// <summary>
        /// Valor del IVA calculado para una unidad.
        /// </summary>
        public decimal ValorIVA { get; set; }

        /// <summary>
        /// Precio total incluyendo IVA.
        /// </summary>
        public decimal PrecioConIVA { get; set; }

        /// <summary>
        /// Indica si el producto tiene stock disponible.
        /// </summary>
        public bool TieneStock { get; set; }

        /// <summary>
        /// Valor total del inventario (Stock × Precio).
        /// </summary>
        public decimal ValorInventario { get; set; }

        /// <summary>
        /// Fecha de creación del producto.
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Fecha de última modificación (puede ser null si nunca se modificó).
        /// </summary>
        public DateTime? FechaModificacion { get; set; }
    }
}