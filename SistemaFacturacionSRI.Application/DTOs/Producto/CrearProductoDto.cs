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
        /// Descripción del producto (opcional).
        /// </summary>
        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Precio unitario sin IVA.
        /// Debe ser mayor a 0.
        /// </summary>
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        /// <summary>
        /// Tipo de IVA aplicable.
        /// Valores válidos: 0 (IVA_0), 12 (IVA_12), 15 (IVA_15)
        /// </summary>
        [Required(ErrorMessage = "El tipo de IVA es obligatorio")]
        [EnumDataType(typeof(TipoIVA), ErrorMessage = "Tipo de IVA inválido. Use 0, 12 o 15")]
        public TipoIVA TipoIVA { get; set; }

        /// <summary>
        /// Cantidad inicial en stock.
        /// Por defecto es 0 si no se especifica.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; } = 0;

        /// <summary>
        /// Unidad de medida del producto.
        /// Por defecto es "Unidad" si no se especifica.
        /// </summary>
        [StringLength(20, ErrorMessage = "La unidad de medida no puede exceder 20 caracteres")]
        public string UnidadMedida { get; set; } = "Unidad";
    }
}