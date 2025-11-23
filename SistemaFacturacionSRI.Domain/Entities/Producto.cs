using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un producto en el sistema de facturación.
    /// Cada instancia corresponde a un registro en la tabla Productos.
    /// </summary>
    public class Producto : EntidadBase
    {
        /// <summary>
        /// Código único del producto (SKU).
        /// Ejemplo: "PROD-001", "LAP-HP-001"
        /// Debe ser único en todo el sistema.
        /// </summary>
        [Required(ErrorMessage = "El código del producto es obligatorio")]
        [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo del producto.
        /// Ejemplo: "Laptop HP Pavilion 15"
        /// </summary>
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
    /// Descripción detallada del producto.
    /// Ejemplo: "Laptop con procesador Intel Core i5, 8GB RAM, 256GB SSD"
    /// </summary>
    [Required(ErrorMessage = "La descripción del producto es obligatoria")]
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Clave foránea opcional a Categoría del producto.
    /// </summary>
    public int? CategoriaId { get; set; }

    /// <summary>
    /// Navegación a Categoría.
    /// </summary>
    public virtual Categoria? Categoria { get; set; }

        /// <summary>
        /// Colección de lotes asociados al producto.
        /// Cada lote representa una entrada de inventario con su propio costo y cantidades.
        /// </summary>
        public ICollection<Lote> Lotes { get; set; } = new List<Lote>();

        /// <summary>
        /// Obtiene el precio actual tomando el costo del lote más reciente.
        /// Retorna null cuando el producto aún no tiene lotes.
        /// </summary>
        public decimal? PrecioActual => Lotes
            .OrderByDescending(l => l.FechaCompra)
            .Select(l => (decimal?)l.PrecioCosto)
            .FirstOrDefault();

        /// <summary>
        /// Calcula el stock disponible sumando la cantidad disponible de cada lote.
        /// </summary>
        public int StockDisponible => Lotes?.Sum(l => l.CantidadDisponible) ?? 0;

        /// <summary>
        /// Indica si existe stock disponible.
        /// </summary>
        public bool TieneStock => StockDisponible > 0;

        /// <summary>
        /// Calcula el valor total del inventario (precio actual × stock disponible).
        /// Retorna null cuando no hay precio vigente.
        /// </summary>
        public decimal? ValorInventario => Lotes != null && Lotes.Any()
            ? Lotes.Sum(l => l.CantidadDisponible * l.PrecioCosto)
            : null;
    }
}