using System;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Controla la secuencia de numeración para cada establecimiento y punto de emisión.
    /// </summary>
    public class SecuenciaFactura
    {
        public int Id { get; set; }
        public string Establecimiento { get; set; } = string.Empty;
        public string PuntoEmision { get; set; } = string.Empty;
        public long SecuenciaActual { get; set; }
        public DateTime? FechaUltimaEmision { get; set; }
        public bool Activo { get; set; } = true;
    }
}
