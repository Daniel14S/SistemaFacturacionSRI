using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class FacturaDetalle
{
    public int FacturaDetalleId { get; set; }

    public int FacturaId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal? Descuento { get; set; }

    public decimal SubtotalLinea { get; set; }

    public decimal IvaLinea { get; set; }

    public decimal TotalLinea { get; set; }

    public virtual Factura Factura { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
