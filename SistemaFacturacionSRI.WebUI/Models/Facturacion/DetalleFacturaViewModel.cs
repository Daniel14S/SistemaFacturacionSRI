// SistemaFacturacionSRI.WebUI/Models/Facturacion/DetalleFacturaViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.WebUI.Models.Facturacion
{
    /// <summary>
    /// ViewModel para detalles de factura (líneas de productos)
    /// T-106: SPRINT 3 - ViewModels de facturación
    /// </summary>
    public class DetalleFacturaViewModel
    {
        // ==================== PRODUCTO ====================
        
        [Required(ErrorMessage = "Debe seleccionar un producto")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido")]
        [Display(Name = "Producto")]
        public int ProductoId { get; set; }
        
        // Información del producto (para mostrar en UI)
        public string? ProductoNombre { get; set; }
        public string? ProductoCodigo { get; set; }
        public decimal ProductoPrecio { get; set; }
        public int ProductoStock { get; set; }
        
        // ==================== LOTE ====================
        
        [Display(Name = "Lote")]
        public int? LoteId { get; set; }
        
        public string? LoteDescripcion { get; set; }
        
        // ==================== CANTIDADES Y PRECIOS ====================
        
        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0.01, 999999.99, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public decimal Cantidad { get; set; } = 1;
        
        [Required(ErrorMessage = "El precio unitario es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [Display(Name = "Precio Unitario")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal PrecioUnitario { get; set; }
        
        [Range(0, 999999.99, ErrorMessage = "El descuento no puede ser negativo")]
        [Display(Name = "Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Descuento { get; set; } = 0;
        
        // ==================== IMPUESTOS ====================
        
        [Required(ErrorMessage = "Debe especificar el código de IVA")]
        [Display(Name = "IVA")]
        public int CodigoPorcentajeIVA { get; set; } = 15; // Por defecto 15%
        
        [Display(Name = "Tarifa IVA")]
        public decimal TarifaIVA => CodigoPorcentajeIVA switch
        {
            0 => 0.00m,
            15 => 0.15m,
            _ => 0.00m
        };
        
        // ==================== TOTALES CALCULADOS ====================
        
        [Display(Name = "Subtotal")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal PrecioTotalSinImpuesto { get; set; }
        
        [Display(Name = "Base Imponible")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal BaseImponible { get; set; }
        
        [Display(Name = "Valor IVA")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal ValorIVA { get; set; }
        
        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal ValorTotal { get; set; }
        
        // ==================== MÉTODOS DE CÁLCULO ====================
        
        /// <summary>
        /// Recalcula todos los totales del detalle
        /// </summary>
        public void RecalcularTotales()
        {
            // Subtotal = Cantidad × Precio Unitario
            PrecioTotalSinImpuesto = Cantidad * PrecioUnitario;
            
            // Base Imponible = Subtotal - Descuento
            BaseImponible = PrecioTotalSinImpuesto - Descuento;
            
            // IVA = Base Imponible × Tarifa
            ValorIVA = BaseImponible * TarifaIVA;
            
            // Total = Base Imponible + IVA
            ValorTotal = BaseImponible + ValorIVA;
        }
        
        /// <summary>
        /// Valida que el detalle tenga datos mínimos
        /// </summary>
        public bool EsValido()
        {
            return ProductoId > 0 && 
                   Cantidad > 0 && 
                   PrecioUnitario > 0 && 
                   Descuento >= 0 &&
                   Cantidad <= ProductoStock;
        }
        
        /// <summary>
        /// Obtiene el nombre de la tarifa de IVA para mostrar en UI
        /// </summary>
        public string ObtenerNombreTarifaIVA()
        {
            return CodigoPorcentajeIVA switch
            {
                0 => "0% (Exento)",
                15 => "15%",
                _ => "No aplicable"
            };
        }
    }
}