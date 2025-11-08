using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Application.DTOs.Producto
{
    /// <summary>
    /// DTO para actualizar un producto existente.
    /// Se usa en peticiones PUT.
    /// Similar a CrearProductoDto pero incluye el Id del producto a actualizar.
    /// </summary>
    public class ActualizarProductoDto
    {
        /// <summary>
        /// Identificador del producto a actualizar.
        /// Obligatorio para saber QUÉ producto modificar.
        /// </summary>
        [Required(ErrorMessage = "El Id del producto es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El Id debe ser mayor a 0")]
        public int Id { get; set; }

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

    }
}