using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Models.XML;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
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

        var webRootPath = ResolverRutaWwwroot();

        // Ruta donde se guardarán los XMLs
        _rutaXml = Path.Combine(webRootPath, "comprobantes", "xml");

        // Crear directorio si no existe
        if (!Directory.Exists(_rutaXml))
        {
            Directory.CreateDirectory(_rutaXml);
            _logger.LogInformation("Directorio para XML creado en {Ruta}", _rutaXml);
        }

        _logger.LogDebug("Ruta de almacenamiento XML configurada en {Ruta}", _rutaXml);
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
            RazonSocialComprador = factura.Cliente.NombreCompleto(),
            IdentificacionComprador = factura.Cliente.Identificacion,
            DireccionComprador = factura.Cliente.Direccion,
            TotalSinImpuestos = factura.Subtotal0 + factura.Subtotal15,
            TotalDescuento = factura.Descuento,
            Propina = factura.Propina,
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
                PrecioTotalSinImpuesto = detalle.PrecioTotalSinImpuesto
            };

            var impuesto = new ImpuestoDetalle
            {
                Codigo = "2", // 2 = IVA
                CodigoPorcentaje = detalle.CodigoPorcentajeIVA.ToString(), // ✅ Convertir a string
                Tarifa = detalle.Tarifa,
                BaseImponible = detalle.PrecioTotalSinImpuesto,
                Valor = detalle.Valor
            };

            detalleXml.Impuestos.Add(impuesto);

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

            // Construir nombre completo del usuario
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

            // Agrupar por CodigoPorcentajeIVA
            var detallesPorCodigo = factura.Detalles
                .GroupBy(d => d.CodigoPorcentajeIVA)
                .OrderBy(g => g.Key);

            foreach (var grupo in detallesPorCodigo)
            {
                var codigoPorcentaje = grupo.Key;
                var baseImponible = grupo.Sum(d => d.PrecioTotalSinImpuesto);
                var valor = grupo.Sum(d => d.Valor);

                var totalImpuesto = new TotalImpuesto
                {
                    Codigo = "2", // 2 = IVA
                    CodigoPorcentaje = codigoPorcentaje.ToString(), // ✅ Convertir a string
                    BaseImponible = baseImponible,
                    Valor = valor
                };

                totalesImpuestos.Add(totalImpuesto);
            }

            return totalesImpuestos;
        }

    /// <summary>
    /// Obtiene el código de porcentaje según la tarifa de IVA
    /// NOTA: Este método ya no es necesario porque CodigoPorcentajeIVA ya está en DetalleFactura
    /// Se mantiene por compatibilidad pero puedes eliminarlo si no se usa en otro lugar
    /// </summary>
    private string ObtenerCodigoPorcentaje(decimal tarifa)
    {
        return tarifa switch
        {
            0m => "0",   // 0%
            12m => "2",  // 12%
            14m => "3",  // 14%
            15m => "4",  // 15%
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

            // Retornar ruta relativa normalizada para exponerla vía HTTP o almacenar en BD
            var rutaRelativa = Path.Combine("comprobantes", "xml", nombreArchivo);
            return rutaRelativa.Replace(Path.DirectorySeparatorChar, '/');
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

        // Verificar que ClaveAcceso no sea null
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

    /// <summary>
    /// Busca la carpeta wwwroot real incluso cuando el proceso se ejecuta desde proyectos distintos.
    /// </summary>
    private string ResolverRutaWwwroot()
    {
        var baseDir = AppContext.BaseDirectory;
        var directorioActual = new DirectoryInfo(baseDir);

        while (directorioActual != null)
        {
            var candidatoLocal = Path.Combine(directorioActual.FullName, "wwwroot");
            if (Directory.Exists(candidatoLocal))
            {
                return candidatoLocal;
            }

            var candidatoWebUi = Path.Combine(directorioActual.FullName, "SistemaFacturacionSRI.WebUI", "wwwroot");
            if (Directory.Exists(candidatoWebUi))
            {
                return candidatoWebUi;
            }

            directorioActual = directorioActual.Parent;
        }

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _logger.LogWarning(
            "No se encontró una carpeta wwwroot existente. Se utilizará {Ruta} como ubicación predeterminada", fallback);
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    // ==================== MÉTODOS ADICIONALES REQUERIDOS POR LA INTERFAZ ====================

    /// <summary>
    /// Genera XML desde un DTO (sobrecarga sin Async en el nombre)
    /// </summary>
    public string GenerarXmlFactura(FacturaDto factura)
    {
        // Implementación síncrona llamando al método async
        return GenerarXmlFacturaAsync(factura.Id).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Valida XML contra esquema XSD (versión con ResultadoValidacion)
    /// </summary>
    public async Task<ResultadoValidacion> ValidarXmlContraEsquema(string xmlContent, string? xsdPath = null)
    {
        var (esValido, errores) = await ValidarXmlContraEsquemaAsync(xmlContent);
        
        if (esValido)
        {
            return ResultadoValidacion.Exitoso();
        }
        
        // Convertir la lista de strings a lista de ErrorValidacion
        var erroresValidacion = errores.Select(error => new ErrorValidacion
        {
            Linea = 0, // No tenemos información de línea en este punto
            Posicion = 0, // No tenemos información de posición
            Mensaje = error,
            Severidad = "Error"
        }).ToList();
        
        return ResultadoValidacion.ConErrores(erroresValidacion);
    }

    /// <summary>
    /// Guarda XML en archivo (sobrecarga compatible con la interfaz)
    /// </summary>
    public async Task<string> GuardarXmlEnArchivo(string xmlContent, string claveAcceso)
    {
        return await GuardarXmlEnArchivoAsync(xmlContent, claveAcceso);
    }

    /// <summary>
    /// Genera y valida XML desde un DTO
    /// </summary>
    public async Task<string> GenerarYValidarXml(FacturaDto factura)
    {
        // Generar XML
        var xmlContent = await GenerarXmlFacturaAsync(factura.Id);
        
        // Validar XML
        var resultado = await ValidarXmlContraEsquema(xmlContent);
        
        if (!resultado.EsValido)
        {
            var erroresDetallados = string.Join(Environment.NewLine, 
                resultado.Errores.Select(e => e.ToString()));
            
            throw new InvalidOperationException(
                $"El XML generado no es válido según el esquema XSD:{Environment.NewLine}{erroresDetallados}");
        }
        
        return xmlContent;
    }

    /// <summary>
    /// T-064: Guarda el XML firmado en el sistema de archivos
    /// Los XMLs firmados se guardan en una subcarpeta "firmados"
    /// </summary>
    /// <param name="xmlFirmado">Contenido del XML firmado</param>
    /// <param name="claveAcceso">Clave de acceso de 49 dígitos</param>
    /// <returns>Ruta relativa donde se guardó el archivo</returns>
    public async Task<string> GuardarXmlFirmadoEnArchivo(string xmlFirmado, string claveAcceso)
    {
        try
        {
            // Validar que el XML tenga firma
            if (!xmlFirmado.Contains("<ds:Signature") && !xmlFirmado.Contains("<Signature"))
            {
                throw new InvalidOperationException("El XML proporcionado no contiene una firma digital válida.");
            }

            // Crear directorio para XMLs firmados si no existe
            var directorioFirmados = Path.Combine(_rutaXml, "firmados");
            if (!Directory.Exists(directorioFirmados))
            {
                Directory.CreateDirectory(directorioFirmados);
            }

            var nombreArchivo = $"{claveAcceso}_firmado.xml";
            var rutaCompleta = Path.Combine(directorioFirmados, nombreArchivo);

            await File.WriteAllTextAsync(rutaCompleta, xmlFirmado, System.Text.Encoding.UTF8);

            return Path.Combine("comprobantes", "xml", "firmados", nombreArchivo);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Error al guardar XML firmado en archivo: {ex.Message}",
                ex
            );
        }
    }
}