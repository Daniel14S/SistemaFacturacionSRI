using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Entidad que representa una categoría de productos.
    /// Ejemplo: Electrónica, Ropa, Alimentos, etc.
    /// </summary>
    public class Categoria : EntidadBase
    {
        /// <summary>
        /// Código único de la categoría.
        /// Ejemplo: "CAT-001", "CAT-ELEC"
        /// </summary>
        [Required(ErrorMessage = "El código de la categoría es obligatorio")]
        [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de la categoría.
        /// Ejemplo: "Electrónica", "Alimentos"
        /// </summary>
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada de la categoría (opcional).
        /// </summary>
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Descripcion { get; set; }

        /// <summary>
        /// Relación de navegación: Productos que pertenecen a esta categoría.
        /// </summary>
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}