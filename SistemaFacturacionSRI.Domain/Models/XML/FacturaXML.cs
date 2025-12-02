using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Domain.Models.XML;

/// <summary>
/// Modelo XML raíz para una factura electrónica completa
/// T-041: SPRINT 3 - DÍA 5
/// 
/// Este es el contenedor principal que agrupa todos los elementos de la factura
/// según el estándar del SRI de Ecuador versión 1.1.0
/// </summary>
[XmlRoot("factura", Namespace = "")]
public class FacturaXML
{
    /// <summary>
    /// Atributo ID requerido por el SRI
    /// Valor fijo: "comprobante"
    /// </summary>
    [XmlAttribute("id")]
    public string Id { get; set; } = "comprobante";

    /// <summary>
    /// Versión del esquema XML de facturas
    /// Valor fijo: "1.1.0"
    /// </summary>
    [XmlAttribute("version")]
    public string Version { get; set; } = "1.1.0";

    /// <summary>
    /// Información tributaria del emisor
    /// Incluye: ambiente, RUC, clave de acceso, etc.
    /// </summary>
    [XmlElement("infoTributaria")]
    public InfoTributaria InfoTributaria { get; set; } = new();

    /// <summary>
    /// Información específica de la factura
    /// Incluye: fecha, comprador, totales, etc.
    /// </summary>
    [XmlElement("infoFactura")]
    public InfoFactura InfoFactura { get; set; } = new();

    /// <summary>
    /// Lista de productos/servicios de la factura
    /// Mínimo 1 detalle
    /// </summary>
    [XmlArray("detalles")]
    [XmlArrayItem("detalle")]
    public List<Detalle> Detalles { get; set; } = new();

    /// <summary>
    /// Información adicional opcional
    /// Hasta 15 campos personalizados
    /// </summary>
    [XmlElement("infoAdicional")]
    public InfoAdicional? InfoAdicional { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public FacturaXML()
    {
        InfoTributaria = new InfoTributaria();
        InfoFactura = new InfoFactura();
        Detalles = new List<Detalle>();
    }

    /// <summary>
    /// Valida que la factura tenga todos los datos mínimos requeridos
    /// </summary>
    /// <returns>Lista de errores de validación (vacía si es válida)</returns>
    public List<string> Validar()
    {
        var errores = new List<string>();

        // Validar InfoTributaria
        if (InfoTributaria == null)
        {
            errores.Add("InfoTributaria es obligatoria");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(InfoTributaria.Ambiente))
                errores.Add("Ambiente es obligatorio");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.TipoEmision))
                errores.Add("TipoEmision es obligatorio");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.RazonSocial))
                errores.Add("RazonSocial es obligatoria");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.Ruc) || InfoTributaria.Ruc.Length != 13)
                errores.Add("RUC debe tener 13 dígitos");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.ClaveAcceso) || InfoTributaria.ClaveAcceso.Length != 48)
                errores.Add("ClaveAcceso debe tener 48 dígitos");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.Estab) || InfoTributaria.Estab.Length != 3)
                errores.Add("Establecimiento debe tener 3 dígitos");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.PtoEmi) || InfoTributaria.PtoEmi.Length != 3)
                errores.Add("PuntoEmision debe tener 3 dígitos");
            
            if (string.IsNullOrWhiteSpace(InfoTributaria.Secuencial) || InfoTributaria.Secuencial.Length != 9)
                errores.Add("Secuencial debe tener 9 dígitos");
        }

        // Validar InfoFactura
        if (InfoFactura == null)
        {
            errores.Add("InfoFactura es obligatoria");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(InfoFactura.FechaEmision))
                errores.Add("FechaEmision es obligatoria");
            
            if (string.IsNullOrWhiteSpace(InfoFactura.RazonSocialComprador))
                errores.Add("RazonSocialComprador es obligatoria");
            
            if (string.IsNullOrWhiteSpace(InfoFactura.IdentificacionComprador))
                errores.Add("IdentificacionComprador es obligatoria");
            
            if (InfoFactura.TotalConImpuestos == null || !InfoFactura.TotalConImpuestos.Any())
                errores.Add("TotalConImpuestos debe tener al menos un impuesto");
            
            if (InfoFactura.ImporteTotal <= 0)
                errores.Add("ImporteTotal debe ser mayor a 0");
        }

        // Validar Detalles
        if (Detalles == null || !Detalles.Any())
        {
            errores.Add("La factura debe tener al menos un detalle");
        }
        else
        {
            for (int i = 0; i < Detalles.Count; i++)
            {
                var detalle = Detalles[i];
                
                if (string.IsNullOrWhiteSpace(detalle.CodigoPrincipal))
                    errores.Add($"Detalle {i + 1}: CodigoPrincipal es obligatorio");
                
                if (string.IsNullOrWhiteSpace(detalle.Descripcion))
                    errores.Add($"Detalle {i + 1}: Descripcion es obligatoria");
                
                if (detalle.Cantidad <= 0)
                    errores.Add($"Detalle {i + 1}: Cantidad debe ser mayor a 0");
                
                if (detalle.PrecioUnitario < 0)
                    errores.Add($"Detalle {i + 1}: PrecioUnitario no puede ser negativo");
                
                if (detalle.Impuestos == null || !detalle.Impuestos.Any())
                    errores.Add($"Detalle {i + 1}: Debe tener al menos un impuesto");
            }
        }

        // Validar InfoAdicional (si existe)
        if (InfoAdicional != null && InfoAdicional.Campos != null)
        {
            if (InfoAdicional.Campos.Count > 15)
                errores.Add("InfoAdicional no puede tener más de 15 campos");
            
            foreach (var campo in InfoAdicional.Campos)
            {
                if (!campo.EsValido())
                    errores.Add($"Campo adicional '{campo.Nombre}' no es válido");
            }
        }

        return errores;
    }

    /// <summary>
    /// Indica si la factura es válida
    /// </summary>
    public bool EsValida => !Validar().Any();

    /// <summary>
    /// Calcula el total de la factura sumando todos los detalles
    /// </summary>
    /// <returns>Total calculado</returns>
    public decimal CalcularTotal()
    {
        if (Detalles == null || !Detalles.Any())
            return 0;

        decimal subtotal = Detalles.Sum(d => d.PrecioTotalSinImpuesto);
        decimal totalImpuestos = Detalles.Sum(d => 
            d.Impuestos?.Sum(i => i.Valor) ?? 0);

        return subtotal + totalImpuestos;
    }

    /// <summary>
    /// Obtiene un resumen de los totales de la factura
    /// </summary>
    public ResumenTotales ObtenerResumenTotales()
    {
        var resumen = new ResumenTotales();

        if (Detalles == null || !Detalles.Any())
            return resumen;

        // Agrupar por tarifa de IVA
        var detallesPorTarifa = Detalles
            .SelectMany(d => d.Impuestos.Select(i => new
            {
                Detalle = d,
                Impuesto = i
            }))
            .GroupBy(x => x.Impuesto.Tarifa);

        foreach (var grupo in detallesPorTarifa)
        {
            var tarifa = grupo.Key;
            var subtotal = grupo.Sum(x => x.Detalle.PrecioTotalSinImpuesto);
            var impuesto = grupo.Sum(x => x.Impuesto.Valor);

            if (tarifa == 0)
            {
                resumen.Subtotal0 = subtotal;
            }
            else if (tarifa == 15)
            {
                resumen.Subtotal15 = subtotal;
                resumen.IVA15 = impuesto;
            }
        }

        resumen.SubtotalTotal = resumen.Subtotal0 + resumen.Subtotal15;
        resumen.TotalDescuento = Detalles.Sum(d => d.Descuento);
        resumen.TotalIVA = resumen.IVA15;
        resumen.Total = resumen.SubtotalTotal - resumen.TotalDescuento + resumen.TotalIVA;

        return resumen;
    }
}

/// <summary>
/// Clase para representar el resumen de totales
/// </summary>
public class ResumenTotales
{
    public decimal Subtotal0 { get; set; }
    public decimal Subtotal15 { get; set; }
    public decimal SubtotalTotal { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal IVA15 { get; set; }
    public decimal TotalIVA { get; set; }
    public decimal Total { get; set; }
}