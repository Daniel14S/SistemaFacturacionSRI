using SistemaFacturacionSRI.Domain.Enums;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Configuración general para la emisión de comprobantes electrónicos de la empresa.
    /// </summary>
    public class ConfiguracionEmpresa
    {
        public int Id { get; set; }
        public string Ruc { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string DirMatriz { get; set; } = string.Empty;
        public string DirEstablecimiento { get; set; } = string.Empty;
        public string? ContribuyenteEspecial { get; set; }
        public bool ObligadoContabilidad { get; set; }
        public string Establecimiento { get; set; } = string.Empty;
        public string PuntoEmision { get; set; } = string.Empty;
        public Ambiente AmbienteSRI { get; set; }
        public TipoEmision TipoEmision { get; set; }
        public string RutaCertificado { get; set; } = string.Empty;
        public string ClaveCertificado { get; set; } = string.Empty;
        public byte[]? Logo { get; set; }
    }
}
