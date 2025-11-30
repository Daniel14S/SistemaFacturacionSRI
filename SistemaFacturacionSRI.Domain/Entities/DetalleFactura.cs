namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Representa cada detalle de una factura con la información requerida por el SRI.
    /// </summary>
    public class DetalleFactura
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public int ProductoId { get; set; }
        public string CodigoPrincipal { get; set; } = string.Empty;
        public string? CodigoAuxiliar { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal PrecioTotalSinImpuesto { get; set; }
        public string CodigoPorcentajeIVA { get; set; } = string.Empty;
        public decimal Tarifa { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal Valor { get; set; }
        public decimal ValorTotal { get; set; }
        public int TarifaIVA { get; set; } // 0, 12, 15
        public decimal ValorIVA { get; set; }

        public Factura? Factura { get; set; }
        public Producto? Producto { get; set; }
    }
}
