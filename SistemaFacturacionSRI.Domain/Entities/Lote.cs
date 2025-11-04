using System;

namespace SistemaFacturacionSRI.Domain.Entities
{
    public class Lote
    {
        public int LoteId { get; set; }
        public int ProductoId { get; set; }
        public DateTime FechaCompra { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public decimal PrecioCosto { get; set; }
        public int CantidadInicial { get; set; }
        public int CantidadDisponible { get; set; }

        public Producto? Producto { get; set; }
    }
}
