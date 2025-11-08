using System;
using System.Collections.Generic; // <-- importante para List<>
using SistemaFacturacionSRI.Application.DTOs.Lote; // <-- necesario para LoteDto
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
        /// Precio unitario sin IVA calculado a partir del último lote registrado.
        /// </summary>
        public decimal? Precio { get; set; }

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
        /// Valor del IVA calculado para una unidad en base al precio actual.
        /// </summary>
        public decimal? ValorIVA { get; set; }

        /// <summary>
        /// Precio total incluyendo IVA (si existe precio base).
        /// </summary>
        public decimal? PrecioConIVA { get; set; }

        /// <summary>
        /// Indica si el producto tiene stock disponible.
        /// </summary>
        public bool TieneStock { get; set; }

        /// <summary>
        /// Valor total del inventario (Stock × Precio actual).
        /// </summary>
        public decimal? ValorInventario { get; set; }

        /// <summary>
        /// Fecha de creación del producto.
        /// </summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Fecha de última modificación (puede ser null si nunca se modificó).
        /// </summary>
        public DateTime? FechaModificacion { get; set; }

        // ===============================
        // NUEVOS CAMPOS: LOTE PRIORITARIO
        // ===============================

        /// <summary>
        /// Código del lote con fecha de expiración más cercana.
        /// </summary>
        public string? LotePrioritario { get; set; }

        /// <summary>
        /// Fecha de expiración del lote prioritario.
        /// </summary>
        public DateTime? FechaExpiracionLotePrioritario { get; set; }

        /// <summary>
        /// Indica si existen diferencias de precios entre lotes.
        /// Se usa para mostrar icono de advertencia en frontend.
        /// </summary>
        public bool TieneVariacionPrecios { get; set; }

        /// <summary>
        /// Precio promedio ponderado de todos los lotes disponibles.
        /// Opcional: se muestra en texto pequeño en la tabla.
        /// </summary>
        public decimal? PrecioCostoPromedio { get; set; }

        /// <summary>
        /// Lista de lotes asociados al producto.
        /// </summary>
        public List<LoteDto> Lotes { get; set; } = new();
        /// <summary>
        /// Porcentaje del IVA en formato decimal (ej: 0.12 para 12%)
        /// </summary>
        public decimal PorcentajeIVA { get; set; }



    }
}
