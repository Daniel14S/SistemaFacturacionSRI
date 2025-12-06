using System;

namespace SistemaFacturacionSRI.Domain.DTOs.Reportes;

public class ReporteVentasMesDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public string NombreMes { get; set; } = string.Empty;
    public int NumeroFacturas { get; set; }
    public decimal TotalFacturado { get; set; }
    public decimal IVARecaudado { get; set; }
}