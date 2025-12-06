using System;

namespace SistemaFacturacionSRI.Domain.DTOs.Reportes;

public class ReporteMejorMesDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public string NombreMes { get; set; } = string.Empty;
    public decimal TotalVentas { get; set; }
    public int NumeroFacturas { get; set; }
    public decimal PromedioFactura { get; set; }
}