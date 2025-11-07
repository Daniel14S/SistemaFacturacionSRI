using SistemaFacturacionSRI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

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
        /// Precio unitario del producto sin IVA.
        /// Ejemplo: 1000.00
        /// Debe ser mayor a cero.
        /// </summary>
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

    /// <summary>
    /// Clave foránea al catálogo de Tipos de IVA almacenado en BD.
    /// Reemplaza el uso del enum TipoIVA y es obligatoria.
    /// </summary>
    [Required(ErrorMessage = "El tipo de IVA es obligatorio")]
    public int TipoIVAId { get; set; }

    /// <summary>
    /// Navegación al catálogo de Tipos de IVA.
    /// </summary>
    public TipoIVACatalogo? TipoIVACatalogo { get; set; }

        /// <summary>
        /// Cantidad disponible en inventario.
        /// Ejemplo: 50 unidades
        /// Puede ser 0 si no hay stock.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; } = 0;

        

    /// <summary>
    /// Clave foránea opcional a Categoría del producto.
    /// </summary>
    public int? CategoriaId { get; set; }

    /// <summary>
    /// Navegación a Categoría.
    /// </summary>
    public virtual Categoria? Categoria { get; set; }

        // Propiedades calculadas (no se guardan en la BD, solo se calculan en memoria)

        /// <summary>
        /// Calcula el valor del IVA para una unidad del producto.
    /// Fórmula: Precio × Porcentaje IVA del catálogo
    /// Ejemplo: Si Precio = 100 y Porcentaje = 12, retorna 12
        /// </summary>
    public decimal ValorIVA => Precio * ((TipoIVACatalogo?.Porcentaje ?? 0m) / 100m);

        /// <summary>
        /// Calcula el precio total (precio base + IVA).
        /// Fórmula: Precio + ValorIVA
        /// Ejemplo: Si Precio = 100 y ValorIVA = 12, retorna 112
        /// </summary>
    public decimal PrecioConIVA => Precio + ValorIVA;

        /// <summary>
        /// Indica si el producto tiene stock disponible.
        /// Útil para validaciones en la interfaz.
        /// </summary>
        public bool TieneStock => Stock > 0;

        /// <summary>
        /// Calcula el valor total del inventario (Stock × Precio).
        /// Ejemplo: Si Stock = 10 y Precio = 100, retorna 1000
        /// </summary>
        public decimal ValorInventario => Stock * Precio;
    }
}