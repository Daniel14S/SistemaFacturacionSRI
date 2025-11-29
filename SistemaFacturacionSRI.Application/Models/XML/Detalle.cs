using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Application.Models.XML;

/// <summary>
/// Modelo XML para un detalle (producto/servicio) de la factura
/// T-039: SPRINT 3 - DÍA 4
/// 
/// Representa un ítem individual en la factura con sus cantidades, precios e impuestos
/// </summary>
[XmlRoot("detalle")]
public class Detalle
{
    /// <summary>
    /// Código principal del producto/servicio
    /// Máximo 25 caracteres
    /// Ejemplo: "PROD-001"
    /// </summary>
    [XmlElement("codigoPrincipal")]
    public string CodigoPrincipal { get; set; } = string.Empty;

    /// <summary>
    /// Código auxiliar del producto (opcional)
    /// Máximo 25 caracteres
    /// Puede ser código de barras, SKU alternativo, etc.
    /// </summary>
    [XmlElement("codigoAuxiliar")]
    public string? CodigoAuxiliar { get; set; }

    /// <summary>
    /// Descripción del producto/servicio
    /// Máximo 300 caracteres
    /// </summary>
    [XmlElement("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad del producto
    /// Formato: decimal con hasta 6 decimales
    /// Debe ser mayor a 0
    /// </summary>
    [XmlElement("cantidad")]
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Precio unitario del producto SIN incluir impuestos
    /// Formato: decimal con hasta 6 decimales
    /// Ejemplo: 15.99
    /// </summary>
    [XmlElement("precioUnitario")]
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Descuento aplicado al producto
    /// Formato: decimal con 2 decimales
    /// Puede ser 0.00 si no hay descuento
    /// </summary>
    [XmlElement("descuento")]
    public decimal Descuento { get; set; }

    /// <summary>
    /// Precio total sin impuestos
    /// Fórmula: (PrecioUnitario * Cantidad) - Descuento
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("precioTotalSinImpuesto")]
    public decimal PrecioTotalSinImpuesto { get; set; }

    /// <summary>
    /// Detalles adicionales del producto (opcional)
    /// Máximo 3 campos adicionales
    /// </summary>
    [XmlArray("detallesAdicionales")]
    [XmlArrayItem("detAdicional")]
    public List<DetalleAdicional>? DetallesAdicionales { get; set; }

    /// <summary>
    /// Lista de impuestos aplicables al producto
    /// Mínimo 1 impuesto (normalmente IVA)
    /// </summary>
    [XmlArray("impuestos")]
    [XmlArrayItem("impuesto")]
    public List<ImpuestoDetalle> Impuestos { get; set; } = new();
}

/// <summary>
/// Modelo para un impuesto aplicado a un detalle
/// </summary>
public class ImpuestoDetalle
{
    /// <summary>
    /// Código del impuesto
    /// 2 = IVA (más común)
    /// 3 = ICE (Impuesto a los Consumos Especiales)
    /// 5 = IRBPNR (Impuesto Redimible a las Botellas Plásticas)
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
    /// Tarifa del impuesto (valor porcentual)
    /// Ejemplos: 0, 12, 15
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("tarifa")]
    public decimal Tarifa { get; set; }

    /// <summary>
    /// Base imponible sobre la cual se calcula el impuesto
    /// Generalmente igual a PrecioTotalSinImpuesto del detalle
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("baseImponible")]
    public decimal BaseImponible { get; set; }

    /// <summary>
    /// Valor del impuesto calculado
    /// Fórmula: BaseImponible * (Tarifa / 100)
    /// Formato: decimal con 2 decimales
    /// </summary>
    [XmlElement("valor")]
    public decimal Valor { get; set; }
}

/// <summary>
/// Modelo para información adicional de un detalle
/// </summary>
public class DetalleAdicional
{
    /// <summary>
    /// Nombre del campo adicional
    /// Ejemplo: "Marca", "Modelo", "Color"
    /// </summary>
    [XmlAttribute("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Valor del campo adicional
    /// Ejemplo: "Samsung", "Galaxy S21", "Negro"
    /// </summary>
    [XmlAttribute("valor")]
    public string Valor { get; set; } = string.Empty;
}