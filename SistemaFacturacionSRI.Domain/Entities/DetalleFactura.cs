namespace SistemaFacturacionSRI.Domain.Entities
{
    public class DetalleFactura
    {
        public int Id { get; set; }
        
        public int FacturaId { get; set; }

        public Factura? Factura { get; set; }
        
        public int? ProductoId { get; set; }
        public Producto? Producto { get; set; }
        
        public string CodigoPrincipal { get; set; } = string.Empty;
        public string? CodigoAuxiliar { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal PrecioTotalSinImpuesto { get; set; }

        public int CodigoPorcentajeIVA { get; set; } 
        public decimal Tarifa { get; set; }  
        public decimal BaseImponible { get; set; }
        public decimal Valor { get; set; } 
        public decimal ValorTotal { get; set; }  

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
    }
}