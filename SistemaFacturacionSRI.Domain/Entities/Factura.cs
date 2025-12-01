// SistemaFacturacionSRI.Domain/Entities/Factura.cs
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Factura
    {
        public int Id { get; set; }
        
        // Información básica
        public string NumeroFactura { get; set; } = string.Empty;
        public string ClaveAcceso { get; set; } = string.Empty;
        
        // Relaciones
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        
        // Colección de detalles
        public ICollection<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();
        
        // CAMBIAR: Usar InfoAdicional en lugar de InformacionAdicional
        public ICollection<InfoAdicional> InfoAdicional { get; set; } = new List<InfoAdicional>();
        
        // Fechas
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        
        // CAMBIAR: Usar ENUMs en lugar de strings
        public Ambiente Ambiente { get; set; }
        public TipoEmision TipoEmision { get; set; }
        public EstadoFactura Estado { get; set; }
        
        // Totales
        public decimal Subtotal0 { get; set; }
        public decimal Subtotal12 { get; set; }
        public decimal Subtotal15 { get; set; }
        public decimal SubtotalNoObjetoIVA { get; set; }
        public decimal SubtotalExentoIVA { get; set; }
        public decimal SubtotalConDescuento { get; set; }
        public decimal Descuento { get; set; }
        public decimal IVA15 { get; set; }
        public decimal Propina { get; set; }
        public decimal ImporteTotal { get; set; }
        
        // Información de autorización SRI
        public string? NumeroAutorizacion { get; set; }
        public DateTime? FechaHoraAutorizacion { get; set; }
        
        // Rutas de archivos
        public string? XmlPath { get; set; }
        public string? XmlFirmadoPath { get; set; }
        public string? PdfPath { get; set; }
        
        // Mensajes y observaciones
        public string? MensajesSRI { get; set; }
        public string? Observaciones { get; set; }
        
        // Auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
    }
}