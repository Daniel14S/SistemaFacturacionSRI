// SistemaFacturacionSRI.WebUI/Models/Facturacion/CrearFacturaViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.WebUI.Models.Facturacion
{
    /// <summary>
    /// ViewModel para crear facturas en el frontend
    /// T-106: SPRINT 3 - ViewModels de facturación
    /// </summary>
    public class CrearFacturaViewModel
    {
        // ==================== CLIENTE ====================
        
        [Required(ErrorMessage = "Debe seleccionar un cliente")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente válido")]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }
        
        // Información del cliente seleccionado (para mostrar en UI)
        public string? ClienteNombre { get; set; }
        public string? ClienteIdentificacion { get; set; }
        public string? ClienteDireccion { get; set; }
        public string? ClienteTelefono { get; set; }
        public string? ClienteEmail { get; set; }
        
        // ==================== DETALLES ====================
        
        [Required(ErrorMessage = "Debe agregar al menos un producto")]
        [MinLength(1, ErrorMessage = "La factura debe tener al menos un producto")]
        public List<DetalleFacturaViewModel> Detalles { get; set; } = new();
        
        // ==================== TOTALES CALCULADOS ====================
        
        [Display(Name = "Subtotal 0%")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal0 { get; set; }
        
        [Display(Name = "Subtotal 15%")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Subtotal15 { get; set; }
        
        [Display(Name = "Subtotal Total")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal SubtotalTotal { get; set; }
        
        [Display(Name = "Total Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal TotalDescuento { get; set; }
        
        [Display(Name = "IVA 15%")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal TotalIVA { get; set; }
        
        [Display(Name = "TOTAL A PAGAR")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public decimal Total { get; set; }
        
        // ==================== INFORMACIÓN ADICIONAL ====================
        
        [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
        
        // ==================== MÉTODOS DE CÁLCULO ====================
        
        /// <summary>
        /// Recalcula todos los totales basándose en los detalles
        /// </summary>
        public void RecalcularTotales()
        {
            Subtotal0 = 0;
            Subtotal15 = 0;
            TotalDescuento = 0;
            TotalIVA = 0;
            
            foreach (var detalle in Detalles)
            {
                detalle.RecalcularTotales();
                
                // Sumar descuentos
                TotalDescuento += detalle.Descuento;
                
                // Agrupar por tarifa de IVA
                if (detalle.CodigoPorcentajeIVA == 0) // 0% IVA
                {
                    Subtotal0 += detalle.BaseImponible;
                }
                else if (detalle.CodigoPorcentajeIVA == 15) // 15% IVA
                {
                    Subtotal15 += detalle.BaseImponible;
                    TotalIVA += detalle.ValorIVA;
                }
            }
            
            SubtotalTotal = Subtotal0 + Subtotal15;
            Total = SubtotalTotal + TotalIVA;
        }
        
        /// <summary>
        /// Agrega un nuevo detalle vacío a la factura
        /// </summary>
        public void AgregarDetalle()
        {
            Detalles.Add(new DetalleFacturaViewModel
            {
                Cantidad = 1,
                Descuento = 0,
                CodigoPorcentajeIVA = 2 // Por defecto 15%
            });
        }
        
        /// <summary>
        /// Elimina un detalle de la factura
        /// </summary>
        public void EliminarDetalle(DetalleFacturaViewModel detalle)
        {
            Detalles.Remove(detalle);
            RecalcularTotales();
        }
        
        /// <summary>
        /// Valida que la factura tenga datos mínimos
        /// </summary>
        public bool EsValida()
        {
            return ClienteId > 0 && Detalles.Any() && Detalles.All(d => d.EsValido());
        }
    }
}