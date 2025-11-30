using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Models.XML;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Application.Services;

/// <summary>
/// Servicio para generar XML de facturas electrónicas según el estándar del SRI
/// T-043 y T-044: SPRINT 3 - DÍA 5
/// </summary>
public class XmlGeneratorService : IXmlGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly XmlValidatorService _xmlValidator;
    private readonly ILogger<XmlGeneratorService> _logger;
    private readonly string _rutaXml;

    public XmlGeneratorService(
        ApplicationDbContext context,
        XmlValidatorService xmlValidator,
        ILogger<XmlGeneratorService> logger)
    {
        _context = context;
        _xmlValidator = xmlValidator;
        _logger = logger;
       
        // Ruta donde se guardarán los XMLs
        _rutaXml = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comprobantes", "xml");
       
        // Crear directorio si no existe
        if (!Directory.Exists(_rutaXml))
        {
            Directory.CreateDirectory(_rutaXml);
        }
    }

    /// <summary>
    /// T-043: Genera el XML completo de una factura
    /// </summary>
    public async Task<string> GenerarXmlFacturaAsync(int facturaId)
    {
        _logger.LogInformation("Generando XML para factura ID: {FacturaId}", facturaId);

        // Generar el objeto FacturaXML
        var facturaXml = await GenerarObjetoFacturaXmlAsync(facturaId);

        // Serializar a XML
        var xmlContent = SerializarFacturaXml(facturaXml);

        _logger.LogInformation("XML generado exitosamente para factura ID: {FacturaId}", facturaId);

        return xmlContent;
    }

    /// <summary>
    /// T-043: Genera el objeto FacturaXML mapeando desde la entidad
    /// </summary>
    public async Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId)
    {
        // Cargar factura completa con todas sus relaciones
        var factura = await _context.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c!.TipoIdentificacion)
            .Include(f => f.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(f => f.Usuario)
            .FirstOrDefaultAsync(f => f.Id == facturaId);

        if (factura == null)
        {
            throw new InvalidOperationException($"No se encontró la factura con ID {facturaId}");
        }

        // Cargar configuración de la empresa
        var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();

        if (configuracion == null)
        {
            throw new InvalidOperationException("No se encontró la configuración de la empresa");
        }

        // Crear el objeto FacturaXML
        var facturaXml = new FacturaXML();

        // ==================== MAPEAR INFO TRIBUTARIA ====================
        facturaXml.InfoTributaria = new InfoTributaria
        {
            Ambiente = configuracion.AmbienteSRI,
            TipoEmision = configuracion.TipoEmision,
            RazonSocial = configuracion.RazonSocial,
            NombreComercial = configuracion.NombreComercial,
            Ruc = configuracion.RUC,
            ClaveAcceso = factura.ClaveAcceso ?? string.Empty,
            CodDoc = "01", // 01 = Factura
            Estab = configuracion.CodigoEstablecimiento,
            PtoEmi = configuracion.PuntoEmision,
            Secuencial = factura.NumeroFactura.Split('-')[2], // Extraer el secuencial
            DirMatriz = configuracion.DirMatriz
        };

        // ==================== MAPEAR INFO FACTURA ====================
        facturaXml.InfoFactura = new InfoFactura
        {
            FechaEmision = factura.FechaEmision.ToString("dd/MM/yyyy"),
            DirEstablecimiento = configuracion.DirEstablecimiento,
            ObligadoContabilidad = configuracion.ObligadoContabilidad ? "SI" : "NO",
            TipoIdentificacionComprador = factura.Cliente!.TipoIdentificacion!.CodigoSRI,
            // CORRECCIÓN: Usar NombreCompleto() en lugar de RazonSocial
            RazonSocialComprador = factura.Cliente.NombreCompleto(),
            IdentificacionComprador = factura.Cliente.Identificacion,
            DireccionComprador = factura.Cliente.Direccion,
            TotalSinImpuestos = factura.Subtotal0 + factura.Subtotal12 + factura.Subtotal15,
            // CORRECCIÓN: Usar Descuento en lugar de TotalDescuento
            TotalDescuento = factura.Descuento,
            Propina = factura.Propina,
            // CORRECCIÓN: Usar ImporteTotal en lugar de Total
            ImporteTotal = factura.ImporteTotal,
            Moneda = "DOLAR"
        };

        // Agregar contribuyente especial si aplica
        if (!string.IsNullOrWhiteSpace(configuracion.AgenteRetencion))
        {
            facturaXml.InfoFactura.ContribuyenteEspecial = configuracion.AgenteRetencion;
        }

        // ==================== T-044: MAPEAR TOTAL CON IMPUESTOS ====================
        facturaXml.InfoFactura.TotalConImpuestos = GenerarTotalConImpuestos(factura);

        // ==================== MAPEAR DETALLES ====================
        foreach (var detalle in factura.Detalles)
        {
            var detalleXml = new Detalle
            {
                CodigoPrincipal = detalle.CodigoPrincipal,
                CodigoAuxiliar = detalle.Producto?.Codigo,
                Descripcion = detalle.Descripcion,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                Descuento = detalle.Descuento,
                // CORRECCIÓN: Usar PrecioTotalSinImpuesto en lugar de Subtotal
                PrecioTotalSinImpuesto = detalle.PrecioTotalSinImpuesto
            };

            // Agregar impuesto (IVA)
            var impuesto = new ImpuestoDetalle
            {
                Codigo = "2", // 2 = IVA
                CodigoPorcentaje = ObtenerCodigoPorcentaje(detalle.TarifaIVA),
                Tarifa = detalle.TarifaIVA,
                // CORRECCIÓN: Usar PrecioTotalSinImpuesto en lugar de Subtotal
                BaseImponible = detalle.PrecioTotalSinImpuesto,
                Valor = detalle.ValorIVA
            };

            detalleXml.Impuestos.Add(impuesto);

            // CORRECCIÓN: DetalleFactura no tiene InformacionAdicional
            // Si necesitas información adicional, deberías agregarla desde otra fuente
            // o agregar esta propiedad a la entidad DetalleFactura

            facturaXml.Detalles.Add(detalleXml);
        }

        // ==================== MAPEAR INFO ADICIONAL ====================
        if (!string.IsNullOrWhiteSpace(factura.Observaciones) ||
            !string.IsNullOrWhiteSpace(factura.Cliente.Email) ||
            !string.IsNullOrWhiteSpace(factura.Cliente.Telefono))
        {
            var infoAdicional = new InfoAdicional();

            // Agregar email del cliente
            if (!string.IsNullOrWhiteSpace(factura.Cliente.Email))
            {
                infoAdicional.AgregarCampo("Email", factura.Cliente.Email);
            }

            // Agregar teléfono del cliente
            if (!string.IsNullOrWhiteSpace(factura.Cliente.Telefono))
            {
                infoAdicional.AgregarCampo("Teléfono", factura.Cliente.Telefono);
            }

            // Agregar observaciones
            if (!string.IsNullOrWhiteSpace(factura.Observaciones))
            {
                infoAdicional.AgregarCampo("Observaciones", factura.Observaciones);
            }

            // CORRECCIÓN: Construir nombre completo del usuario
            if (factura.Usuario != null)
            {
                var nombreVendedor = $"{factura.Usuario.Nombre1} {factura.Usuario.Nombre2} {factura.Usuario.Apellido1} {factura.Usuario.Apellido2}".Trim();
                infoAdicional.AgregarCampo("Vendedor", nombreVendedor);
            }

            facturaXml.InfoAdicional = infoAdicional;
        }

        return facturaXml;
    }

    /// <summary>
    /// T-044: Genera la sección TotalConImpuestos agrupando por tarifa
    /// </summary>
    private List<TotalImpuesto> GenerarTotalConImpuestos(Domain.Entities.Factura factura)
    {
        var totalesImpuestos = new List<TotalImpuesto>();

        // Agrupar detalles por tarifa de IVA
        var detallesPorTarifa = factura.Detalles
            .GroupBy(d => d.TarifaIVA)
            .OrderBy(g => g.Key); // Ordenar por tarifa (0%, 12%, 15%)

        foreach (var grupo in detallesPorTarifa)
        {
            var tarifa = grupo.Key;
            // CORRECCIÓN: Usar PrecioTotalSinImpuesto en lugar de Subtotal
            var baseImponible = grupo.Sum(d => d.PrecioTotalSinImpuesto);
            var valor = grupo.Sum(d => d.ValorIVA);

            var totalImpuesto = new TotalImpuesto
            {
                Codigo = "2", // 2 = IVA
                CodigoPorcentaje = ObtenerCodigoPorcentaje(tarifa),
                BaseImponible = baseImponible,
                Valor = valor
            };

            totalesImpuestos.Add(totalImpuesto);
        }

        return totalesImpuestos;
    }

    /// <summary>
    /// Obtiene el código de porcentaje según la tarifa de IVA
    /// </summary>
    private string ObtenerCodigoPorcentaje(int tarifa)
    {
        return tarifa switch
        {
            0 => "0",   // 0%
            12 => "2",  // 12%
            14 => "3",  // 14%
            15 => "4",  // 15%
            _ => throw new InvalidOperationException($"Tarifa de IVA no soportada: {tarifa}")
        };
    }

    /// <summary>
    /// Serializa el objeto FacturaXML a string XML
    /// </summary>
    public string SerializarFacturaXml(FacturaXML facturaXml)
    {
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(FacturaXML));
           
            // Configurar settings para XML
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false), // UTF-8 sin BOM
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
           
            // Crear namespaces vacíos para evitar xmlns adicionales
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            // Serializar
            xmlSerializer.Serialize(xmlWriter, facturaXml, namespaces);

            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al serializar FacturaXML");
            throw new InvalidOperationException("Error al generar el XML de la factura", ex);
        }
    }

    /// <summary>
    /// Deserializa un XML string a objeto FacturaXML
    /// </summary>
    public FacturaXML DeserializarFacturaXml(string xmlContent)
    {
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(FacturaXML));

            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader);

            var facturaXml = (FacturaXML)xmlSerializer.Deserialize(xmlReader)!;

            return facturaXml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al deserializar XML");
            throw new InvalidOperationException("Error al leer el XML de la factura", ex);
        }
    }

    /// <summary>
    /// Valida el XML contra el esquema XSD
    /// </summary>
    public async Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent)
    {
        return await Task.Run(() =>
        {
            var esValido = _xmlValidator.ValidarXmlContraEsquema(xmlContent, out var errores);
            return (esValido, errores);
        });
    }

    /// <summary>
    /// Guarda el XML en un archivo físico
    /// </summary>
    public async Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso)
    {
        try
        {
            // Nombrar el archivo con la clave de acceso
            var nombreArchivo = $"{claveAcceso}.xml";
            var rutaCompleta = Path.Combine(_rutaXml, nombreArchivo);

            // Guardar el archivo
            await File.WriteAllTextAsync(rutaCompleta, xmlContent, Encoding.UTF8);

            _logger.LogInformation("XML guardado en: {Ruta}", rutaCompleta);

            // Retornar la ruta relativa para guardar en BD
            return Path.Combine("comprobantes", "xml", nombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar archivo XML");
            throw new InvalidOperationException("Error al guardar el archivo XML", ex);
        }
    }

    /// <summary>
    /// Genera y guarda el XML en un solo paso
    /// </summary>
    public async Task<(string XmlContent, string RutaArchivo)> GenerarYGuardarXmlAsync(int facturaId)
    {
        // Generar XML
        var xmlContent = await GenerarXmlFacturaAsync(facturaId);

        // Obtener clave de acceso
        var factura = await _context.Facturas.FindAsync(facturaId);

        if (factura == null)
        {
            throw new InvalidOperationException($"No se encontró la factura con ID {facturaId}");
        }

        // CORRECCIÓN: Verificar que ClaveAcceso no sea null
        if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
        {
            throw new InvalidOperationException($"La factura con ID {facturaId} no tiene clave de acceso generada");
        }

        // Guardar archivo
        var rutaArchivo = await GuardarXmlEnArchivoAsync(xmlContent, factura.ClaveAcceso);

        // Actualizar la ruta en la BD
        factura.XmlPath = rutaArchivo;
        await _context.SaveChangesAsync();

        return (xmlContent, rutaArchivo);
    }
}