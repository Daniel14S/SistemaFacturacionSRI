// SistemaFacturacionSRI.WebUI/Models/Facturacion/FiltroFacturaViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.WebUI.Models.Facturacion
{
    /// <summary>
    /// ViewModel para filtrar facturas en el historial
    /// T-106: SPRINT 3 - ViewModels de facturación
    /// </summary>
    public class FiltroFacturaViewModel
    {
        // ==================== PAGINACIÓN ====================
        
        [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0")]
        public int PageNumber { get; set; } = 1;
        
        [Range(5, 100, ErrorMessage = "El tamaño de página debe estar entre 5 y 100")]
        public int PageSize { get; set; } = 10;
        
        // ==================== FILTROS ====================
        
        [Display(Name = "Número de Factura")]
        [MaxLength(50)]
        public string? NumeroFactura { get; set; }
        
        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }
        
        [Display(Name = "Usuario/Vendedor")]
        public int? UsuarioId { get; set; }
        
        [Display(Name = "Estado")]
        public string? Estado { get; set; }
        
        [Display(Name = "Fecha Desde")]
        [DataType(DataType.Date)]
        public DateTime? FechaDesde { get; set; }
        
        [Display(Name = "Fecha Hasta")]
        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; }
        
        // ==================== ORDENAMIENTO ====================
        
        [Display(Name = "Ordenar por")]
        public string OrdenCampo { get; set; } = "FechaEmision";
        
        [Display(Name = "Orden")]
        public string OrdenDireccion { get; set; } = "DESC"; // ASC o DESC
        
        // ==================== MÉTODOS AUXILIARES ====================
        
        /// <summary>
        /// Limpia todos los filtros
        /// </summary>
        public void LimpiarFiltros()
        {
            NumeroFactura = null;
            ClienteId = null;
            UsuarioId = null;
            Estado = null;
            FechaDesde = null;
            FechaHasta = null;
            PageNumber = 1;
        }
        
        /// <summary>
        /// Verifica si hay algún filtro aplicado
        /// </summary>
        public bool TieneFiltrosActivos()
        {
            return !string.IsNullOrWhiteSpace(NumeroFactura) ||
                   ClienteId.HasValue ||
                   UsuarioId.HasValue ||
                   !string.IsNullOrWhiteSpace(Estado) ||
                   FechaDesde.HasValue ||
                   FechaHasta.HasValue;
        }
        
        /// <summary>
        /// Valida el rango de fechas
        /// </summary>
        public bool RangoFechasValido()
        {
            if (!FechaDesde.HasValue || !FechaHasta.HasValue)
                return true;
                
            return FechaDesde.Value <= FechaHasta.Value;
        }
    }
}