using System;

namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Factura
    {
        public int FacturaId { get; set; }
        public int ClienteId { get; set; }
        public int UsuarioId { get; set; }

        public string NumeroFactura { get; set; } = string.Empty; // UNIQUE
        public DateTime FechaEmision { get; set; }

        public string? ClaveAccesoSRI { get; set; } // UNIQUE
        public string EstadoSRI { get; set; } = "Pendiente";
        public string Estado { get; set; } = "Emitida";

        public decimal Subtotal { get; set; }
        public decimal TotalIVA { get; set; }
        public decimal Total { get; set; }

        public Cliente? Cliente { get; set; }
        public Usuario? Usuario { get; set; }
        public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
    }
}
