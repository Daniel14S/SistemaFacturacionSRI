using SistemaFacturacionSRI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Producto
{
    /// <summary>
    /// DTO para crear un nuevo producto.
    /// Se usa en peticiones POST.
    /// Contiene solo los datos que el cliente debe enviar para crear.
    /// </summary>
    public class CrearProductoDto
    {
        /// <summary>
        /// Código único del producto (SKU).
        /// Obligatorio y debe ser único en el sistema.
        /// </summary>
        [Required(ErrorMessage = "El código del producto es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El código debe tener entre 3 y 50 caracteres")]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "El código solo puede contener letras mayúsculas, números y guiones")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del producto.
        /// Obligatorio.
        /// </summary>
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
    /// Descripción del producto.
    /// Obligatoria.
    /// </summary>
    [Required(ErrorMessage = "La descripción del producto es obligatoria")]
    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario sin IVA.
        /// Debe ser mayor a 0.
        /// </summary>
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

    /// <summary>
    /// Tipo de IVA aplicable (FK del catálogo TiposIVA).
    /// </summary>
    [Required(ErrorMessage = "El tipo de IVA es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un tipo de IVA válido")]
    public int TipoIVAId { get; set; }

        /// <summary>
        /// Categoría del producto.
        /// </summary>
        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría válida")]
        public int CategoriaId { get; set; }

        /// <summary>
        /// Cantidad inicial en stock.
        /// Por defecto es 0 si no se especifica.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; } = 0;

        
    }
}