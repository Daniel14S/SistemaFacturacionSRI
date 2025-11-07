namespace SistemaFacturacionSRI.Domain.Entities
{
    public class FacturaDetalle
    {
        public int FacturaDetalleId { get; set; }
        public int FacturaId { get; set; }
        public int ProductoId { get; set; }

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    // Descuento opcional por línea (nullable)
    public decimal? Descuento { get; set; }
        public decimal SubtotalLinea { get; set; }
        public decimal IvaLinea { get; set; }
        public decimal TotalLinea { get; set; }

        public Factura? Factura { get; set; }
        public Producto? Producto { get; set; }
    }
}
