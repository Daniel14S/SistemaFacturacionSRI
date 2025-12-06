using System;

namespace SistemaFacturacionSRI.Domain.DTOs.Reportes;

public class ReporteProductoMasVendidoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal MontoTotal { get; set; }
    public int NumeroFacturas { get; set; }
}