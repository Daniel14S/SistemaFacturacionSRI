using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Application.Models.XML;

/// <summary>
/// Modelo XML para la sección InfoTributaria del comprobante electrónico
/// T-037: SPRINT 3 - DÍA 3
/// 
/// Contiene información del emisor y datos tributarios básicos
/// </summary>
[XmlRoot("infoTributaria")]
public class InfoTributaria
{
    /// <summary>
    /// Ambiente del SRI
    /// 1 = Pruebas
    /// 2 = Producción
    /// </summary>
    [XmlElement("ambiente")]
    public string Ambiente { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de emisión
    /// 1 = Emisión normal
    /// 2 = Emisión por indisponibilidad del sistema
    /// </summary>
    [XmlElement("tipoEmision")]
    public string TipoEmision { get; set; } = string.Empty;

    /// <summary>
    /// Razón social del emisor
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("razonSocial")]
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del emisor
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("nombreComercial")]
    public string NombreComercial { get; set; } = string.Empty;

    /// <summary>
    /// RUC del emisor (13 dígitos)
    /// </summary>
    [XmlElement("ruc")]
    public string Ruc { get; set; } = string.Empty;

    /// <summary>
    /// Clave de acceso del comprobante (48 dígitos)
    /// </summary>
    [XmlElement("claveAcceso")]
    public string ClaveAcceso { get; set; } = string.Empty;

    /// <summary>
    /// Código del tipo de documento
    /// 01 = Factura
    /// 04 = Nota de Crédito
    /// 05 = Nota de Débito
    /// 06 = Guía de Remisión
    /// 07 = Comprobante de Retención
    /// </summary>
    [XmlElement("codDoc")]
    public string CodDoc { get; set; } = "01";

    /// <summary>
    /// Código del establecimiento (3 dígitos)
    /// Ejemplo: "001"
    /// </summary>
    [XmlElement("estab")]
    public string Estab { get; set; } = string.Empty;

    /// <summary>
    /// Código del punto de emisión (3 dígitos)
    /// Ejemplo: "001"
    /// </summary>
    [XmlElement("ptoEmi")]
    public string PtoEmi { get; set; } = string.Empty;

    /// <summary>
    /// Número secuencial del comprobante (9 dígitos)
    /// Ejemplo: "000000001"
    /// </summary>
    [XmlElement("secuencial")]
    public string Secuencial { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de la matriz del emisor
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("dirMatriz")]
    public string DirMatriz { get; set; } = string.Empty;
}