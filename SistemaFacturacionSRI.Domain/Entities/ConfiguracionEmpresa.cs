namespace SistemaFacturacionSRI.Domain.Entities;

/// <summary>
/// Entidad que almacena la configuración de la empresa emisora de facturas electrónicas
/// </summary>
public class ConfiguracionEmpresa : EntidadBase
{
    // ==================== DATOS DE LA EMPRESA ====================
    
    /// <summary>
    /// RUC de la empresa (13 dígitos)
    /// </summary>
    public string RUC { get; set; } = string.Empty;
    
    /// <summary>
    /// Razón social de la empresa
    /// </summary>
    public string RazonSocial { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre comercial de la empresa
    /// </summary>
    public string NombreComercial { get; set; } = string.Empty;
    
    /// <summary>
    /// Dirección de la matriz
    /// </summary>
    public string DirMatriz { get; set; } = string.Empty;
    
    /// <summary>
    /// Dirección del establecimiento emisor
    /// </summary>
    public string DirEstablecimiento { get; set; } = string.Empty;
    
    /// <summary>
    /// Código del establecimiento (3 dígitos, ej: "001")
    /// </summary>
    public string CodigoEstablecimiento { get; set; } = string.Empty;
    
    /// <summary>
    /// Punto de emisión (3 dígitos, ej: "001")
    /// </summary>
    public string PuntoEmision { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si la empresa está obligada a llevar contabilidad
    /// </summary>
    public bool ObligadoContabilidad { get; set; }
    
    /// <summary>
    /// Resolución de agente de retención (null si no aplica)
    /// Ejemplo: "1" si es agente de retención
    /// </summary>
    public string? AgenteRetencion { get; set; }
    
    // ==================== DATOS DE CONTACTO ====================
    
    /// <summary>
    /// Teléfono de contacto
    /// </summary>
    public string Telefono { get; set; } = string.Empty;
    
    /// <summary>
    /// Email de contacto
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    // ==================== CONFIGURACIÓN CERTIFICADO DIGITAL ====================
    
    /// <summary>
    /// Ruta del archivo del certificado digital (.p12 o .pfx)
    /// Ejemplo: "wwwroot/certificados/certificado.p12"
    /// </summary>
    public string? RutaCertificadoDigital { get; set; }
    
    /// <summary>
    /// Contraseña del certificado digital (debe estar encriptada)
    /// </summary>
    public string? ClaveCertificadoDigital { get; set; }
    
    // ==================== CONFIGURACIÓN SRI ====================
    
    /// <summary>
    /// Ambiente del SRI: "1" = Pruebas, "2" = Producción
    /// </summary>
    public string AmbienteSRI { get; set; } = "1";
    
    /// <summary>
    /// Tipo de emisión: "1" = Normal, "2" = Indisponibilidad
    /// </summary>
    public string TipoEmision { get; set; } = "1";
    
    /// <summary>
    /// URL del WebService de Recepción de Comprobantes del SRI
    /// </summary>
    public string UrlRecepcionComprobantes { get; set; } = string.Empty;
    
    /// <summary>
    /// URL del WebService de Autorización de Comprobantes del SRI
    /// </summary>
    public string UrlAutorizacionComprobantes { get; set; } = string.Empty;
    
    // ==================== CONFIGURACIÓN VISUAL ====================
    
    /// <summary>
    /// Ruta del logo de la empresa para el RIDE
    /// Ejemplo: "wwwroot/images/logo.png"
    /// </summary>
    public string? LogoPath { get; set; }
    
    /// <summary>
    /// Información adicional que se incluirá por defecto en las facturas
    /// Formato: "Campo1|Campo2|Campo3"
    /// Ejemplo: "Gracias por su compra|Consulte en www.empresa.com"
    /// </summary>
    public string? InfoAdicionalDefecto { get; set; }
}

