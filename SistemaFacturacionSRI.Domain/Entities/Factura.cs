using System;
using System.Collections.Generic;
using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Representa la factura electrónica con toda la información requerida por el SRI.
    /// </summary>
    public class Factura
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty; // UNIQUE
        public string? ClaveAcceso { get; set; }
        public int ClienteId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public Ambiente Ambiente { get; set; }
        public TipoEmision TipoEmision { get; set; }
        public EstadoFactura Estado { get; set; }
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
        public string? NumeroAutorizacion { get; set; }
        public DateTime? FechaHoraAutorizacion { get; set; }
        public string? XmlPath { get; set; }
        public string? XmlFirmadoPath { get; set; }
        public string? PdfPath { get; set; }
        public string? MensajesSRI { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public Cliente? Cliente { get; set; }
        public Usuario? Usuario { get; set; }
        public ICollection<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();
        public ICollection<InfoAdicional> InformacionAdicional { get; set; } = new List<InfoAdicional>();
    }
}
