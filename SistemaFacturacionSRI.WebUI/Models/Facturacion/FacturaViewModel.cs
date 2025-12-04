// SistemaFacturacionSRI.WebUI/Models/Facturacion/FacturaViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.WebUI.Models.Facturacion
{
    /// <summary>
    /// ViewModel para visualizar facturas en el frontend
    /// T-106: SPRINT 3 - ViewModels de facturación
    /// </summary>
    public class FacturaViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Número de Factura")]
        public string NumeroFactura { get; set; } = string.Empty;
        
        [Display(Name = "Clave de Acceso")]
        public string ClaveAcceso { get; set; } = string.Empty;
        
        [Display(Name = "Fecha de Emisión")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaEmision { get; set; }
        
        [Display(Name = "Estado")]
        public string Estado { get; set; } = string.Empty;
        
        public string EstadoDescripcion { get; set; } = string.Empty;
        
        // Cliente
        public int ClienteId { get; set; }
        
        [Display(Name = "Cliente")]
        public string ClienteNombre { get; set; } = string.Empty;
        
        public string ClienteIdentificacion { get; set; } = string.Empty;
        
        // Usuario
        public int UsuarioId { get; set; }
        
        [Display(Name = "Vendedor")]
        public string? UsuarioNombre { get; set; }
        
        // Totales
        [Display(Name = "Subtotal 0%")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Subtotal0 { get; set; }
        
        [Display(Name = "Subtotal 15%")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Subtotal15 { get; set; }
        
        [Display(Name = "Subtotal Total")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal SubtotalTotal { get; set; }
        
        [Display(Name = "Descuento")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal TotalDescuento { get; set; }
        
        [Display(Name = "IVA")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal TotalIVA { get; set; }
        
        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }
        
        // Archivos
        public string? XmlPath { get; set; }
        public string? XmlFirmadoPath { get; set; }
        public string? PdfPath { get; set; }
        
        // SRI
        [Display(Name = "Número de Autorización")]
        public string? NumeroAutorizacion { get; set; }
        
        [Display(Name = "Fecha de Autorización")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FechaAutorizacion { get; set; }
        
        public string? MensajesSRI { get; set; }
        
        public string? Observaciones { get; set; }
        
        // Auditoría
        [Display(Name = "Fecha de Creación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaCreacion { get; set; }
        
        public DateTime? FechaModificacion { get; set; }
        
        // Propiedades computadas para el UI
        public string EstadoColor => Estado switch
        {
            "BORRADOR" => "secondary",
            "GENERADA" => "info",
            "FIRMADA" => "primary",
            "ENVIADA" => "warning",
            "RECIBIDA" => "info",
            "AUTORIZADA" => "success",
            "NO_AUTORIZADA" => "danger",
            "DEVUELTA" => "warning",
            "ANULADA" => "dark",
            _ => "secondary"
        };
        
        public string EstadoIcono => Estado switch
        {
            "BORRADOR" => "bi-file-earmark",
            "GENERADA" => "bi-file-earmark-text",
            "FIRMADA" => "bi-file-earmark-lock",
            "ENVIADA" => "bi-send",
            "RECIBIDA" => "bi-inbox",
            "AUTORIZADA" => "bi-check-circle-fill",
            "NO_AUTORIZADA" => "bi-x-circle-fill",
            "DEVUELTA" => "bi-arrow-return-left",
            "ANULADA" => "bi-slash-circle",
            _ => "bi-file-earmark"
        };
        
        public bool PuedeEditar => Estado == "BORRADOR";
        public bool PuedeFirmar => Estado == "BORRADOR" || Estado == "GENERADA";
        public bool PuedeEnviarSRI => Estado == "FIRMADA";
        public bool PuedeReenviar => Estado == "DEVUELTA" || Estado == "NO_AUTORIZADA";
        public bool PuedeAnular => Estado == "AUTORIZADA";
        public bool PuedeDescargarPDF => !string.IsNullOrEmpty(XmlFirmadoPath) || Estado == "AUTORIZADA";
        public bool PuedeDescargarXML => !string.IsNullOrEmpty(XmlPath);
    }
}