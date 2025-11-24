using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class Factura
{
    public int FacturaId { get; set; }

    public int ClienteId { get; set; }

    public int UsuarioId { get; set; }

    public string NumeroFactura { get; set; } = null!;

    public DateTime FechaEmision { get; set; }

    public string? ClaveAccesoSri { get; set; }

    public string EstadoSri { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal TotalIva { get; set; }

    public decimal Total { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<FacturaDetalle> FacturaDetalles { get; set; } = new List<FacturaDetalle>();

    public virtual Usuario Usuario { get; set; } = null!;
}
