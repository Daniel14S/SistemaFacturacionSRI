namespace SistemaFacturacionSRI.Application.DTOs.Configuracion;

/// <summary>
/// DTO para lectura de configuración de la empresa
/// </summary>
public class ConfiguracionEmpresaDto
{
    public int Id { get; set; }

    // Datos de la empresa
    public string RUC { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string DirMatriz { get; set; } = string.Empty;
    public string DirEstablecimiento { get; set; } = string.Empty;
    public string CodigoEstablecimiento { get; set; } = string.Empty;
    public string PuntoEmision { get; set; } = string.Empty;
    public bool ObligadoContabilidad { get; set; }
    public string? AgenteRetencion { get; set; }

    // Contacto
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Certificado Digital
    public bool TieneCertificado { get; set; }
    public string? RutaCertificadoDigital { get; set; }
    public InfoCertificadoDto? InfoCertificado { get; set; }

    // Configuración SRI
    public string AmbienteSRI { get; set; } = string.Empty;
    public string AmbienteSRIDescripcion { get; set; } = string.Empty;
    public string TipoEmision { get; set; } = string.Empty;
    public string UrlRecepcionComprobantes { get; set; } = string.Empty;
    public string UrlAutorizacionComprobantes { get; set; } = string.Empty;

    // Visual
    public string? LogoPath { get; set; }
    public string? InfoAdicionalDefecto { get; set; }

    // Estado
    public bool ConfiguracionCompleta { get; set; }
    public List<string>? CamposFaltantes { get; set; }
}

/// <summary>
/// DTO para actualizar configuración
/// </summary>
public class ActualizarConfiguracionDto
{
    public string RazonSocial { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string DirMatriz { get; set; } = string.Empty;
    public string DirEstablecimiento { get; set; } = string.Empty;
    public bool ObligadoContabilidad { get; set; }
    public string? AgenteRetencion { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? InfoAdicionalDefecto { get; set; }
}

/// <summary>
/// DTO con información del certificado digital
/// </summary>
public class InfoCertificadoDto
{
    public string Titular { get; set; } = string.Empty;
    public string RucTitular { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool EstaVigente { get; set; }
    public int DiasParaExpirar { get; set; }
    public string Emisor { get; set; } = string.Empty;
}