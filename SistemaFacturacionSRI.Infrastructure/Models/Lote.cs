using System;
using System.Collections.Generic;

namespace SistemaFacturacionSRI.Infrastructure.Models;

public partial class Lote
{
    public int LoteId { get; set; }

    public int ProductoId { get; set; }

    public DateTime FechaCompra { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public decimal PrecioCosto { get; set; }

    public decimal Pvp { get; set; }

    public int CantidadInicial { get; set; }

    public int CantidadDisponible { get; set; }

    public virtual Producto Producto { get; set; } = null!;
}
