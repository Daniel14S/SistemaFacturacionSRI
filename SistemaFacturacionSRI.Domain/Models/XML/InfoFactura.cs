using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Domain.Models.XML;

/// <summary>
/// Modelo XML para la sección InfoFactura del comprobante electrónico
/// T-038: SPRINT 3 - DÍA 3
/// 
/// Contiene información específica de la factura y del comprador
/// </summary>
[XmlRoot("infoFactura")]
public class InfoFactura
{
    /// <summary>
    /// Fecha de emisión de la factura
    /// Formato: dd/MM/yyyy
    /// Ejemplo: "27/11/2024"
    /// </summary>
    [XmlElement("fechaEmision")]
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>
    /// Dirección del establecimiento emisor
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("dirEstablecimiento")]
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>
    /// Número de resolución de contribuyente especial
    /// Campo opcional, solo si aplica
    /// </summary>
    [XmlElement("contribuyenteEspecial")]
    public string? ContribuyenteEspecial { get; set; }

    /// <summary>
    /// Obligado a llevar contabilidad
    /// "SI" o "NO"
    /// </summary>
    [XmlElement("obligadoContabilidad")]
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de identificación del comprador
    /// 04 = RUC
    /// 05 = Cédula
    /// 06 = Pasaporte
    /// 07 = Consumidor Final
    /// 08 = Identificación del Exterior
    /// </summary>
    [XmlElement("tipoIdentificacionComprador")]
    public string TipoIdentificacionComprador { get; set; } = string.Empty;

    /// <summary>
    /// Razón social o nombre del comprador
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("razonSocialComprador")]
    public string RazonSocialComprador { get; set; } = string.Empty;

    /// <summary>
    /// Número de identificación del comprador
    /// Máximo 20 caracteres
    /// </summary>
    [XmlElement("identificacionComprador")]
    public string IdentificacionComprador { get; set; } = string.Empty;

    /// <summary>
    /// Dirección del comprador (opcional)
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("direccionComprador")]
    public string? DireccionComprador { get; set; }

    /// <summary>
    /// Suma de todos los subtotales sin impuestos
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("totalSinImpuestos")]
    public decimal TotalSinImpuestos { get; set; }

    /// <summary>
    /// Total de descuentos aplicados
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("totalDescuento")]
    public decimal TotalDescuento { get; set; }

    /// <summary>
    /// Lista de totales agrupados por tipo y tarifa de impuesto
    /// </summary>
    [XmlArray("totalConImpuestos")]
    [XmlArrayItem("totalImpuesto")]
    public List<TotalImpuesto> TotalConImpuestos { get; set; } = new();

    /// <summary>
    /// Propina (siempre 0.00 para facturas normales)
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("propina")]
    public decimal Propina { get; set; } = 0.00m;

    /// <summary>
    /// Importe total de la factura
    /// TotalSinImpuestos - TotalDescuento + Suma(TotalConImpuestos) + Propina
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("importeTotal")]
    public decimal ImporteTotal { get; set; }

    /// <summary>
    /// Moneda (siempre "DOLAR" en Ecuador)
    /// </summary>
    [XmlElement("moneda")]
    public string Moneda { get; set; } = "DOLAR";

    /// <summary>
    /// Formas de pago (opcional)
    /// </summary>
    [XmlArray("pagos")]
    [XmlArrayItem("pago")]
    public List<Pago>? Pagos { get; set; }
}

/// <summary>
/// Modelo para el total de un tipo de impuesto
/// </summary>
public class TotalImpuesto
{
    /// <summary>
    /// Código del impuesto
    /// 2 = IVA
    /// 3 = ICE
    /// 5 = IRBPNR
    /// </summary>
    [XmlElement("codigo")]
    public string Codigo { get; set; } = "2"; // Por defecto IVA

    /// <summary>
    /// Código del porcentaje del impuesto
    /// Para IVA:
    /// 0 = 0%
    /// 2 = 12%
    /// 3 = 14%
    /// 4 = 15%
    /// 6 = No objeto de IVA
    /// 7 = Exento de IVA
    /// </summary>
    [XmlElement("codigoPorcentaje")]
    public string CodigoPorcentaje { get; set; } = string.Empty;

    /// <summary>
    /// Base imponible sobre la cual se calcula el impuesto
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("baseImponible")]
    public decimal BaseImponible { get; set; }

    /// <summary>
    /// Valor del impuesto calculado
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("valor")]
    public decimal Valor { get; set; }
}

/// <summary>
/// Modelo para forma de pago (opcional)
/// </summary>
public class Pago
{
    /// <summary>
    /// Forma de pago
    /// 01 = Sin utilización del sistema financiero
    /// 16 = Tarjeta de débito
    /// 17 = Dinero electrónico
    /// 18 = Tarjeta prepago
    /// 19 = Tarjeta de crédito
    /// 20 = Otros con utilización del sistema financiero
    /// </summary>
    [XmlElement("formaPago")]
    public string FormaPago { get; set; } = "01";

    /// <summary>
    /// Total pagado con esta forma de pago
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Plazo de pago en días (opcional, para crédito)
    /// </summary>
    [XmlElement("plazo")]
    public string? Plazo { get; set; }

    /// <summary>
    /// Unidad de tiempo del plazo (opcional)
    /// Ejemplo: "dias", "meses"
    /// </summary>
    [XmlElement("unidadTiempo")]
    public string? UnidadTiempo { get; set; }
}