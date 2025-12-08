// SistemaFacturacionSRI.Infrastructure/Services/XmlGeneratorService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.Models.XML;
using SistemaFacturacionSRI.Infrastructure.Data;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio unificado para generar y validar XMLs de facturas electrónicas según estándar SRI
    /// Combina funcionalidad de generación desde BD y desde DTOs
    /// </summary>
    public class XmlGeneratorService : IXmlGeneratorService
    {
        private readonly ILogger<XmlGeneratorService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly string _xsdBasePath;
        private readonly string _xmlOutputPath;

        public XmlGeneratorService(
            ILogger<XmlGeneratorService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

            // Buscar wwwroot real del proyecto, no de bin/Release
            var wwwrootPath = ResolverRutaWwwroot();

            _xsdBasePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "XSD"
            );

            // Ruta para guardar XMLs generados
            _xmlOutputPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "wwwroot",
                "comprobantes",
                "xml"
            );

            if (!Directory.Exists(_xsdBasePath))
            {
                Directory.CreateDirectory(_xsdBasePath);
            }

            if (!Directory.Exists(_xmlOutputPath))
            {
                Directory.CreateDirectory(_xmlOutputPath);
                _logger.LogInformation("Directorio para XML creado en {Ruta}", _xmlOutputPath);
            }

            _logger.LogInformation("XmlGeneratorService inicializado");
            _logger.LogInformation("   - XSD Path: {XsdPath}", _xsdBasePath);
            _logger.LogInformation("   - XML Output Path: {XmlPath}", _xmlOutputPath);
        }

        /// <summary>
        /// Busca la carpeta wwwroot real del proyecto, NO la de bin/Release/net8.0
        /// </summary>
        private string ResolverRutaWwwroot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logger.LogDebug("🔍 Buscando wwwroot desde: {BaseDir}", baseDir);

            // Estrategia 1: Usar Directory.GetCurrentDirectory() que apunta al proyecto
            var currentDirWwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (Directory.Exists(currentDirWwwroot))
            {
                _logger.LogInformation("✅ wwwroot encontrado en CurrentDirectory: {Ruta}", currentDirWwwroot);
                return currentDirWwwroot;
            }

            // Estrategia 2: Subir desde bin/Release/net8.0 hasta encontrar el proyecto
            var directorioActual = new DirectoryInfo(baseDir);

            while (directorioActual != null)
            {
                // IMPORTANTE: NO aceptar wwwroot dentro de bin/
                if (!directorioActual.FullName.Contains("\\bin\\"))
                {
                    var candidatoLocal = Path.Combine(directorioActual.FullName, "wwwroot");
                    if (Directory.Exists(candidatoLocal))
                    {
                        _logger.LogInformation("✅ wwwroot encontrado (fuera de bin): {Ruta}", candidatoLocal);
                        return candidatoLocal;
                    }
                }

                // También buscar SistemaFacturacionSRI.WebUI/wwwroot
                var candidatoWebUi = Path.Combine(directorioActual.FullName, "SistemaFacturacionSRI.WebUI", "wwwroot");
                if (Directory.Exists(candidatoWebUi))
                {
                    _logger.LogInformation("✅ wwwroot encontrado en WebUI: {Ruta}", candidatoWebUi);
                    return candidatoWebUi;
                }

                directorioActual = directorioActual.Parent;
            }

            // Estrategia 3: Fallback - crear en CurrentDirectory
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _logger.LogWarning("⚠️ No se encontró wwwroot existente. Creando en: {Ruta}", fallback);

            if (!Directory.Exists(fallback))
            {
                Directory.CreateDirectory(fallback);
            }

            return fallback;
        }

        // ============================================================
        // GENERACIÓN XML DESDE BASE DE DATOS (Para Application Layer)
        // ============================================================

        /// <summary>
        /// T-043: Genera el XML completo de una factura desde BD
        /// Usa formato manual XDocument para cumplir con estándar SRI
        /// </summary>
        public async Task<string> GenerarXmlFacturaAsync(int facturaId)
        {
            _logger.LogInformation("📄 Generando XML para factura ID: {FacturaId}", facturaId);

            try
            {
                // ✅ USAR FORMATO MANUAL (XDocument) - UTF-8, version 2.1.0
                var xmlContent = await GenerarXmlManualAsync(facturaId);

                // ❌ NO USAR: Serialización que genera UTF-16 y version 1.1.0
                // var facturaXml = await GenerarObjetoFacturaXmlAsync(facturaId);
                // var xmlContent = SerializarFacturaXml(facturaXml);

                _logger.LogInformation("✅ XML generado exitosamente para factura ID: {FacturaId}", facturaId);
                _logger.LogInformation("   📏 Tamaño: {Tamaño} caracteres", xmlContent.Length);

                return xmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar XML para factura {FacturaId}", facturaId);
                throw;
            }
        }

        /// <summary>
        /// Genera XML en formato manual usando XDocument (UTF-8, version 2.1.0)
        /// </summary>
        private async Task<string> GenerarXmlManualAsync(int facturaId)
        {
            _logger.LogDebug("Generando XML (formato manual) para factura ID: {FacturaId}", facturaId);

            // Cargar factura completa
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

            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
            if (configuracion == null)
            {
                throw new InvalidOperationException("No se encontró la configuración de la empresa");
            }

            var culture = CultureInfo.InvariantCulture;
            var (establecimiento, puntoEmision, secuencial) = ObtenerDatosEmision(factura.NumeroFactura);

            // Determinar razón social del comprador
            string razonSocialComprador = factura.Cliente!.Identificacion == "9999999999999"
                ? "CONSUMIDOR FINAL"
                : factura.Cliente.NombreCompleto().Trim();

            var totalSinImpuestos = factura.Subtotal0 + factura.Subtotal15;

            // Construir documento XML manualmente
            var documento = new XDocument(
                new XElement("factura",
                    new XAttribute("id", "comprobante"),
                    new XAttribute("version", "2.1.0"),

                    // InfoTributaria
                    new XElement("infoTributaria",
                        new XElement("ambiente", configuracion.AmbienteSRI),
                        new XElement("tipoEmision", configuracion.TipoEmision),
                        new XElement("razonSocial", configuracion.RazonSocial),
                        new XElement("nombreComercial", configuracion.NombreComercial),
                        new XElement("ruc", configuracion.RUC),
                        new XElement("claveAcceso", factura.ClaveAcceso ?? string.Empty),
                        new XElement("codDoc", "01"),
                        new XElement("estab", establecimiento),
                        new XElement("ptoEmi", puntoEmision),
                        new XElement("secuencial", secuencial),
                        new XElement("dirMatriz", configuracion.DirMatriz)
                    ),

                    // InfoFactura
                    new XElement("infoFactura",
                        new XElement("fechaEmision", factura.FechaEmision.ToString("dd/MM/yyyy")),
                        new XElement("dirEstablecimiento", configuracion.DirEstablecimiento),

                        // Contribuyente especial si existe
                        !string.IsNullOrWhiteSpace(configuracion.AgenteRetencion)
                            ? new XElement("contribuyenteEspecial", configuracion.AgenteRetencion)
                            : null,

                        new XElement("obligadoContabilidad", configuracion.ObligadoContabilidad ? "SI" : "NO"),
                        new XElement("tipoIdentificacionComprador", factura.Cliente.TipoIdentificacion!.CodigoSRI),
                        new XElement("razonSocialComprador", razonSocialComprador),
                        new XElement("identificacionComprador", factura.Cliente.Identificacion),

                        // Dirección si existe
                        !string.IsNullOrWhiteSpace(factura.Cliente.Direccion)
                            ? new XElement("direccionComprador", factura.Cliente.Direccion)
                            : null,

                        new XElement("totalSinImpuestos", totalSinImpuestos.ToString("F2", culture)),
                        new XElement("totalDescuento", factura.Descuento.ToString("F2", culture)),

                        // TotalConImpuestos
                        new XElement("totalConImpuestos", GenerarTotalImpuestosManualXML(factura.Detalles, culture)),

                        new XElement("propina", factura.Propina.ToString("F2", culture)),
                        new XElement("importeTotal", factura.ImporteTotal.ToString("F2", culture)),
                        new XElement("moneda", "DOLAR")
                    ),

                    // Detalles
                    new XElement("detalles", GenerarDetallesManualXML(factura.Detalles, culture)),

                    // InfoAdicional (si existe)
                    GenerarInfoAdicionalManualXML(factura)
                )
            );

            return ConvertirXDocumentAStringSeguro(documento);
        }

        /// <summary>
        /// Genera TotalConImpuestos en formato manual
        /// </summary>
        private IEnumerable<XElement> GenerarTotalImpuestosManualXML(
            ICollection<Domain.Entities.DetalleFactura> detalles,
            CultureInfo culture)
        {
            var grupos = detalles
                .GroupBy(d => d.CodigoPorcentajeIVA)
                .Select(g => new
                {
                    CodigoPorcentaje = g.Key,
                    BaseImponible = g.Sum(d => d.PrecioTotalSinImpuesto),
                    Valor = g.Sum(d => d.Valor)
                })
                .ToList();

            foreach (var grupo in grupos)
            {
                yield return new XElement("totalImpuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", grupo.CodigoPorcentaje.ToString()),
                    new XElement("baseImponible", grupo.BaseImponible.ToString("F2", culture)),
                    new XElement("valor", grupo.Valor.ToString("F2", culture))
                );
            }
        }

        /// <summary>
        /// Genera Detalles en formato manual
        /// </summary>
        private IEnumerable<XElement> GenerarDetallesManualXML(
            ICollection<Domain.Entities.DetalleFactura> detalles,
            CultureInfo culture)
        {
            foreach (var detalle in detalles)
            {
                yield return new XElement("detalle",
                    new XElement("codigoPrincipal", detalle.CodigoPrincipal),

                    // CodigoAuxiliar opcional
                    !string.IsNullOrWhiteSpace(detalle.Producto?.Codigo)
                        ? new XElement("codigoAuxiliar", detalle.Producto.Codigo)
                        : null,

                    new XElement("descripcion", detalle.Descripcion),
                    new XElement("cantidad", detalle.Cantidad.ToString(culture)),
                    new XElement("precioUnitario", detalle.PrecioUnitario.ToString("F6", culture)),
                    new XElement("descuento", detalle.Descuento.ToString("F2", culture)),
                    new XElement("precioTotalSinImpuesto", detalle.PrecioTotalSinImpuesto.ToString("F2", culture)),

                    // Impuestos
                    new XElement("impuestos",
                        new XElement("impuesto",
                            new XElement("codigo", "2"),
                            new XElement("codigoPorcentaje", detalle.CodigoPorcentajeIVA.ToString()),
                            new XElement("tarifa", detalle.Tarifa.ToString(culture)),
                            new XElement("baseImponible", detalle.PrecioTotalSinImpuesto.ToString("F2", culture)),
                            new XElement("valor", detalle.Valor.ToString("F2", culture))
                        )
                    )
                );
            }
        }

        /// <summary>
        /// Genera InfoAdicional en formato manual
        /// </summary>
        private XElement? GenerarInfoAdicionalManualXML(Domain.Entities.Factura factura)
        {
            var campos = new List<XElement>();

            // Email
            if (!string.IsNullOrWhiteSpace(factura.Cliente?.Email))
            {
                campos.Add(new XElement("campoAdicional",
                    new XAttribute("nombre", "Email"),
                    factura.Cliente.Email
                ));
            }

            // Teléfono
            if (!string.IsNullOrWhiteSpace(factura.Cliente?.Telefono))
            {
                campos.Add(new XElement("campoAdicional",
                    new XAttribute("nombre", "Teléfono"),
                    factura.Cliente.Telefono
                ));
            }

            // Observaciones
            if (!string.IsNullOrWhiteSpace(factura.Observaciones))
            {
                campos.Add(new XElement("campoAdicional",
                    new XAttribute("nombre", "Observaciones"),
                    factura.Observaciones
                ));
            }

            // Vendedor
            if (factura.Usuario != null)
            {
                var nombreVendedor = $"{factura.Usuario.Nombre1} {factura.Usuario.Nombre2} {factura.Usuario.Apellido1} {factura.Usuario.Apellido2}".Trim();
                campos.Add(new XElement("campoAdicional",
                    new XAttribute("nombre", "Vendedor"),
                    nombreVendedor
                ));
            }

            // Solo retornar infoAdicional si hay campos
            return campos.Any() ? new XElement("infoAdicional", campos) : null;
        }

        /// <summary>
        /// T-043: Genera el objeto FacturaXML mapeando desde la entidad en BD
        /// </summary>
        public async Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId)
        {
            _logger.LogInformation("🔍 Cargando factura {FacturaId} desde BD...", facturaId);

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

            _logger.LogInformation("✅ Factura cargada: {NumeroFactura}", factura.NumeroFactura);

            // Cargar configuración de la empresa
            var configuracion = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();

            if (configuracion == null)
            {
                throw new InvalidOperationException("No se encontró la configuración de la empresa");
            }

            _logger.LogInformation("✅ Configuración de empresa cargada: {RazonSocial}", configuracion.RazonSocial);

            // Crear el objeto FacturaXML
            var facturaXml = new FacturaXML();

            _logger.LogInformation("📋 Construyendo InfoTributaria...");
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
                Secuencial = factura.NumeroFactura.Split('-')[2],
                DirMatriz = configuracion.DirMatriz
            };

            _logger.LogInformation("📝 Construyendo InfoFactura...");
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

            if (!string.IsNullOrWhiteSpace(configuracion.AgenteRetencion))
            {
                facturaXml.InfoFactura.ContribuyenteEspecial = configuracion.AgenteRetencion;
            }

            _logger.LogInformation("💰 Generando TotalConImpuestos...");
            // ==================== MAPEAR TOTAL CON IMPUESTOS ====================
            facturaXml.InfoFactura.TotalConImpuestos = GenerarTotalConImpuestos(factura);

            _logger.LogInformation("📦 Procesando {CantidadDetalles} detalles...", factura.Detalles.Count);
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
                    CodigoPorcentaje = detalle.CodigoPorcentajeIVA.ToString(),
                    Tarifa = detalle.Tarifa,
                    BaseImponible = detalle.PrecioTotalSinImpuesto,
                    Valor = detalle.Valor
                };

                detalleXml.Impuestos.Add(impuesto);
                facturaXml.Detalles.Add(detalleXml);
            }

            _logger.LogInformation("✅ {CantidadDetalles} detalles procesados", factura.Detalles.Count);

            // ==================== MAPEAR INFO ADICIONAL ====================
            if (!string.IsNullOrWhiteSpace(factura.Observaciones) ||
                !string.IsNullOrWhiteSpace(factura.Cliente.Email) ||
                !string.IsNullOrWhiteSpace(factura.Cliente.Telefono))
            {
                var infoAdicional = new InfoAdicional();

                if (!string.IsNullOrWhiteSpace(factura.Cliente.Email))
                {
                    infoAdicional.AgregarCampo("Email", factura.Cliente.Email);
                }

                if (!string.IsNullOrWhiteSpace(factura.Cliente.Telefono))
                {
                    infoAdicional.AgregarCampo("Teléfono", factura.Cliente.Telefono);
                }

                if (!string.IsNullOrWhiteSpace(factura.Observaciones))
                {
                    infoAdicional.AgregarCampo("Observaciones", factura.Observaciones);
                }

                if (factura.Usuario != null)
                {
                    var nombreVendedor = $"{factura.Usuario.Nombre1} {factura.Usuario.Nombre2} {factura.Usuario.Apellido1} {factura.Usuario.Apellido2}".Trim();
                    infoAdicional.AgregarCampo("Vendedor", nombreVendedor);
                }

                facturaXml.InfoAdicional = infoAdicional;
            }

            _logger.LogInformation("✅ Objeto FacturaXML construido exitosamente");

            return facturaXml;
        }

        /// <summary>
        /// T-044: Genera la sección TotalConImpuestos agrupando por tarifa
        /// </summary>
        private List<TotalImpuesto> GenerarTotalConImpuestos(Domain.Entities.Factura factura)
        {
            var totalesImpuestos = new List<TotalImpuesto>();

            var detallesPorCodigo = factura.Detalles
                .GroupBy(d => d.CodigoPorcentajeIVA)
                .OrderBy(g => g.Key);

            foreach (var grupo in detallesPorCodigo)
            {
                var codigoPorcentaje = grupo.Key;
                var baseImponible = grupo.Sum(d => d.PrecioTotalSinImpuesto);
                var valor = grupo.Sum(d => d.Valor);

                _logger.LogInformation("   💵 Impuesto: Código {Codigo}, Base ${Base:F2}, Valor ${Valor:F2}",
                    codigoPorcentaje, baseImponible, valor);

                var totalImpuesto = new TotalImpuesto
                {
                    Codigo = "2", // 2 = IVA
                    CodigoPorcentaje = codigoPorcentaje.ToString(),
                    BaseImponible = baseImponible,
                    Valor = valor
                };

                totalesImpuestos.Add(totalImpuesto);
            }

            return totalesImpuestos;
        }

        // ============================================================
        // GENERACIÓN XML DESDE DTO (Para WebUI y tests)
        // ============================================================

        /// <summary>
        /// Genera el XML de una factura desde un DTO
        /// </summary>
        public string GenerarXmlFactura(FacturaDto factura)
        {
            try
            {
                _logger.LogDebug("Generando XML desde DTO para factura {NumeroFactura}", factura.NumeroFactura);

                ArgumentNullException.ThrowIfNull(factura);

                if (factura.Detalles == null || !factura.Detalles.Any())
                {
                    throw new InvalidOperationException("La factura debe tener al menos un detalle para generar el XML.");
                }

                var culture = CultureInfo.InvariantCulture;
                var (establecimiento, puntoEmision, secuencial) = ObtenerDatosEmision(factura.NumeroFactura);

                var cliente = factura.Cliente ?? new ClienteFacturaDto
                {
                    TipoIdentificacion = "07",
                    Identificacion = "9999999999999",
                    RazonSocial = "CONSUMIDOR FINAL",
                    Direccion = "NO DEFINIDA"
                };

                var fechaEmision = factura.FechaEmision == default ? DateTime.Now : factura.FechaEmision;

                if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
                {
                    factura.ClaveAcceso = GenerarClaveAcceso(fechaEmision, establecimiento, puntoEmision, secuencial);
                }

                var totalSinImpuestos = factura.Detalles.Sum(d => d.PrecioTotalSinImpuesto);
                var totalDescuento = factura.Detalles.Sum(d => d.Descuento);
                var totalImpuestos = factura.Detalles.Sum(d => d.Valor);
                var totalFactura = totalSinImpuestos - totalDescuento + totalImpuestos;

                string razonSocialComprador = cliente.Identificacion == "9999999999999"
                    ? cliente.RazonSocial
                    : cliente.RazonSocial?.Trim() ?? "SIN NOMBRE";

                var documento = new XDocument(
                    new XElement("factura",
                        new XAttribute("id", "comprobante"),
                        new XAttribute("version", "2.1.0"),
                        ConstruirInfoTributaria(factura.ClaveAcceso, establecimiento, puntoEmision, secuencial),
                        ConstruirInfoFactura(fechaEmision, cliente, razonSocialComprador, totalSinImpuestos, totalDescuento, totalFactura, factura.Detalles, culture),
                        ConstruirDetalles(factura.Detalles, culture)
                    )
                );

                return ConvertirXDocumentAStringSeguro(documento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar XML desde DTO");
                throw new InvalidOperationException($"Error al generar XML: {ex.Message}", ex);
            }
        }

        private static XElement ConstruirInfoTributaria(string claveAcceso, string estab, string ptoEmi, string secuencial)
        {
            var clave = string.IsNullOrWhiteSpace(claveAcceso) ? new string('0', 49) : claveAcceso;

            return new XElement("infoTributaria",
                new XElement("ambiente", "1"),
                new XElement("tipoEmision", "1"),
                new XElement("razonSocial", "EMPRESA DE PRUEBA S.A."),
                new XElement("nombreComercial", "EMPRESA DE PRUEBA"),
                new XElement("ruc", "1804183794001"),
                new XElement("claveAcceso", clave),
                new XElement("codDoc", "01"),
                new XElement("estab", estab),
                new XElement("ptoEmi", ptoEmi),
                new XElement("secuencial", secuencial),
                new XElement("dirMatriz", "AV. PRINCIPAL 123")
            );
        }

        private static XElement ConstruirInfoFactura(
            XNamespace ns,
            DateTime fechaEmision,
            ClienteFacturaDto cliente,
            string razonSocialComprador,
            decimal totalSinImpuestos,
            decimal totalDescuento,
            decimal totalFactura,
            IEnumerable<DetalleFacturaDto> detalles,
            CultureInfo culture)
        {
            var totalesImpuestos = GenerarTotalImpuestosXML(detalles, culture);

            return new XElement("infoFactura",
                new XElement("fechaEmision", fechaEmision.ToString("dd/MM/yyyy")),
                new XElement("dirEstablecimiento", "AV. PRINCIPAL 123"),
                new XElement("tipoIdentificacionComprador", cliente.TipoIdentificacion),
                new XElement("razonSocialComprador", razonSocialComprador),
                new XElement("identificacionComprador", cliente.Identificacion),
                new XElement("totalSinImpuestos", totalSinImpuestos.ToString("F2", culture)),
                new XElement("totalDescuento", totalDescuento.ToString("F2", culture)),
                new XElement("totalConImpuestos", totalesImpuestos),
                new XElement("propina", "0.00"),
                new XElement("importeTotal", totalFactura.ToString("F2", culture)),
                new XElement("moneda", "DOLAR")
            );
        }

        private static IEnumerable<XElement> GenerarTotalImpuestosXML(IEnumerable<DetalleFacturaDto> detalles, CultureInfo culture)
        {
            var grupos = detalles
    .GroupBy(d => new { d.CodigoPorcentajeIVA, d.Tarifa })
    .Select(g => new
    {
        // Determinamos el código de porcentaje usando la tarifa correctamente
        CodigoPorcentaje = DeterminarCodigoPorcentajeIVA(g.Key.Tarifa),  // Aseguramos que se use la tarifa para determinar el código
        g.Key.Tarifa,
        BaseImponible = g.Sum(x => x.BaseImponible),
        Valor = g.Sum(x => x.Valor)
    })
    .ToList();

            if (!grupos.Any())
            {
                yield return new XElement("totalImpuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", "0"),
                    new XElement("baseImponible", "0.00"),
                    new XElement("valor", "0.00")
                );
            }

            foreach (var grupo in grupos)
            {
                yield return new XElement("totalImpuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", grupo.CodigoPorcentaje),
                    new XElement("baseImponible", grupo.BaseImponible.ToString("F2", culture)),
                    new XElement("valor", grupo.Valor.ToString("F2", culture))
                );
            }
        }

        private static XElement ConstruirDetalles(IEnumerable<DetalleFacturaDto> detalles, CultureInfo culture)
        {
            return new XElement("detalles", GenerarDetallesXML(detalles, culture));
        }

        private static IEnumerable<XElement> GenerarDetallesXML(IEnumerable<DetalleFacturaDto> detalles, CultureInfo culture)
        {
            foreach (var detalle in detalles)
            {
                var codigoPorcentaje = DeterminarCodigoPorcentajeIVA(detalle.Tarifa);
                var tarifaFormateada = FormatearTarifa(detalle.Tarifa);

                yield return new XElement("detalle",
                    new XElement("codigoPrincipal", detalle.CodigoPrincipal),
                    detalle.CodigoAuxiliar != null ? new XElement("codigoAuxiliar", detalle.CodigoAuxiliar) : null,
                    new XElement("descripcion", detalle.Descripcion),
                    new XElement("cantidad", detalle.Cantidad.ToString(culture)),
                    new XElement("precioUnitario", detalle.PrecioUnitario.ToString("F6", culture)),
                    new XElement("descuento", detalle.Descuento.ToString("F2", culture)),
                    new XElement("precioTotalSinImpuesto", detalle.PrecioTotalSinImpuesto.ToString("F2", culture)),
                    new XElement("impuestos",
                        new XElement("impuesto",
                            new XElement("codigo", "2"),
                            new XElement("codigoPorcentaje", codigoPorcentaje),
                            new XElement("tarifa", tarifaFormateada),
                            new XElement("baseImponible", detalle.BaseImponible.ToString("F2", culture)),
                            new XElement("valor", detalle.Valor.ToString("F2", culture))
                        )
                    )
                );
            }
        }

        private static string FormatearTarifa(decimal tarifa)
        {
            var tarifaNormalizada = tarifa >= 1 ? tarifa : tarifa * 100;
            return ((int)Math.Round(tarifaNormalizada)).ToString();
        }

        private static string DeterminarCodigoPorcentajeIVA(decimal tarifa)
        {
            var tarifaNormalizada = tarifa >= 1 ? tarifa : tarifa * 100;

            return tarifaNormalizada switch
            {
                0 => "0",   // IVA 0%
                5 => "5",   // IVA 5%
                12 => "2",  // IVA 12%
                15 => "4",  // IVA 15% ← CORRECTO PARA 2025
                _ => "0"    // Valor por defecto (0%)
            };
        }


        private string ConvertirXDocumentAStringSeguro(XDocument documento)
        {
            using (var ms = new MemoryStream())
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),  // UTF-8 sin BOM
                    Indent = true,                       // ✅ CON indentación
                    IndentChars = "  ",                  // 2 espacios
                    NewLineHandling = NewLineHandling.Replace,
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(ms, settings))
                {
                    documento.Save(writer);
                }

                byte[] buffer = ms.ToArray();
                string xmlString = new UTF8Encoding(false).GetString(buffer);

                // Eliminar BOM si existe
                if (xmlString.Length > 0 && xmlString[0] == '\uFEFF')
                    xmlString = xmlString.Substring(1);

                return xmlString;
            }
        }

        private static (string Establecimiento, string PuntoEmision, string Secuencial) ObtenerDatosEmision(string numeroFactura)
        {
            if (!string.IsNullOrWhiteSpace(numeroFactura))
            {
                var partes = numeroFactura.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 3)
                {
                    return (
                        partes[0].PadLeft(3, '0'),
                        partes[1].PadLeft(3, '0'),
                        partes[2].PadLeft(9, '0'));  // MANTENER formato de 9 dígitos
                }
            }

            return ("001", "001", "000000001");
        }

        private string GenerarClaveAcceso(DateTime fechaEmision, string establecimiento, string puntoEmision, string secuencial)
        {
            var fecha = fechaEmision.ToString("ddMMyyyy");
            var tipo = "01";
            var ruc = "1804183794001";
            var ambiente = "1";
            var codigoNumerico = "12345678";
            var tipoEmision = "1";

            var claveBase = $"{fecha}{tipo}{ruc}{ambiente}{establecimiento}{puntoEmision}{secuencial}{codigoNumerico}{tipoEmision}";

            if (claveBase.Length != 48)
            {
                throw new InvalidOperationException($"Clave base debe tener 48 dígitos, tiene {claveBase.Length}");
            }

            var digitoVerificador = CalcularDigitoVerificador(claveBase);
            return claveBase + digitoVerificador;
        }

        private int CalcularDigitoVerificador(string claveBase)
        {
            int factor = 7;
            int suma = 0;

            for (int i = 0; i < claveBase.Length; i++)
            {
                int digito = int.Parse(claveBase[i].ToString());
                suma += digito * factor;
                factor = factor == 2 ? 7 : factor - 1;
            }

            int residuo = suma % 11;
            return residuo == 0 ? 0 : 11 - residuo;
        }

        // ============================================================
        // SERIALIZACIÓN Y DESERIALIZACIÓN
        // ============================================================

        public string SerializarFacturaXml(FacturaXML facturaXml)
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(FacturaXML));

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = new UTF8Encoding(false),
                    OmitXmlDeclaration = false
                };

                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, settings);

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");

                xmlSerializer.Serialize(xmlWriter, facturaXml, namespaces);

                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al serializar FacturaXML");
                throw new InvalidOperationException("Error al generar el XML de la factura", ex);
            }
        }

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

        // ============================================================
        // VALIDACIÓN XML
        // ============================================================

        public async Task<ResultadoValidacion> ValidarXmlContraEsquema(
            string xmlContent,
            string? xsdPath = null)
        {
            var errores = new List<ErrorValidacion>();

            try
            {
                xsdPath ??= Path.Combine(_xsdBasePath, "factura_v2.1.0.xsd");

                if (!File.Exists(xsdPath))
                {
                    return ResultadoValidacion.ConErrores(new List<ErrorValidacion>
                    {
                        new ErrorValidacion
                        {
                            Linea = 0,
                            Posicion = 0,
                            Mensaje = $"No se encontró el esquema XSD en: {xsdPath}",
                            Severidad = "Error"
                        }
                    });
                }

                var resolver = new XmlUrlResolver();
                var schemas = new XmlSchemaSet { XmlResolver = resolver };
                schemas.Add("", xsdPath);

                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas,
                    ValidationFlags =
                        XmlSchemaValidationFlags.ReportValidationWarnings |
                        XmlSchemaValidationFlags.ProcessSchemaLocation |
                        XmlSchemaValidationFlags.ProcessInlineSchema,
                    Async = true,
                    XmlResolver = resolver
                };

                settings.ValidationEventHandler += (sender, args) =>
                {
                    errores.Add(new ErrorValidacion
                    {
                        Linea = args.Exception?.LineNumber ?? 0,
                        Posicion = args.Exception?.LinePosition ?? 0,
                        Mensaje = args.Message,
                        Severidad = args.Severity == XmlSeverityType.Error ? "Error" : "Advertencia"
                    });
                };

                using (var stringReader = new StringReader(xmlContent))
                using (var reader = XmlReader.Create(stringReader, settings))
                {
                    while (await reader.ReadAsync()) { }
                }

                if (errores.Any(e => e.Severidad == "Error"))
                {
                    return ResultadoValidacion.ConErrores(errores);
                }

                if (errores.Any())
                {
                    return new ResultadoValidacion
                    {
                        EsValido = true,
                        Errores = errores,
                        Mensaje = $"XML válido con {errores.Count} advertencia(s)"
                    };
                }

                return ResultadoValidacion.Exitoso();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar XML");
                return ResultadoValidacion.ConErrores(new List<ErrorValidacion>
                {
                    new ErrorValidacion
                    {
                        Linea = 0,
                        Posicion = 0,
                        Mensaje = $"Error al validar XML: {ex.Message}",
                        Severidad = "Error"
                    }
                });
            }
        }

        public async Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent)
        {
            var resultado = await ValidarXmlContraEsquema(xmlContent);
            var errores = resultado.Errores.Select(e => e.ToString()).ToList();
            return (resultado.EsValido, errores);
        }

        public async Task<string> GenerarYValidarXml(FacturaDto factura)
        {
            try
            {
                var xmlContent = GenerarXmlFactura(factura);
                var resultado = await ValidarXmlContraEsquema(xmlContent);

                if (!resultado.EsValido)
                {
                    var mensajesError = string.Join("\n",
                        resultado.Errores.Select(e => e.ToString()));

                    throw new InvalidOperationException(
                        $"El XML generado no cumple con el esquema XSD del SRI:\n{mensajesError}"
                    );
                }

                return xmlContent;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al generar y validar XML: {ex.Message}",
                    ex
                );
            }
        }

        // ============================================================
        // GUARDADO DE ARCHIVOS
        // ============================================================

        public async Task<string> GuardarXmlEnArchivo(string xmlContent, string claveAcceso)
        {
            try
            {
                var nombreArchivo = $"{claveAcceso}.xml";
                var rutaCompleta = Path.Combine(_xmlOutputPath, nombreArchivo);

                await File.WriteAllTextAsync(rutaCompleta, xmlContent, Encoding.UTF8);

                _logger.LogInformation("XML guardado en: {Ruta}", rutaCompleta);

                return Path.Combine("comprobantes", "xml", nombreArchivo).Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar XML en archivo");
                throw new InvalidOperationException(
                    $"Error al guardar XML en archivo: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso)
        {
            return await GuardarXmlEnArchivo(xmlContent, claveAcceso);
        }

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
                var directorioFirmados = Path.Combine(_xmlOutputPath, "firmados");
                if (!Directory.Exists(directorioFirmados))
                {
                    Directory.CreateDirectory(directorioFirmados);
                }

                var nombreArchivo = $"{claveAcceso}_firmado.xml";
                var rutaCompleta = Path.Combine(directorioFirmados, nombreArchivo);

                await File.WriteAllTextAsync(rutaCompleta, xmlFirmado, Encoding.UTF8);

                _logger.LogInformation("XML firmado guardado en: {Ruta}", rutaCompleta);

                return Path.Combine("comprobantes", "xml", "firmados", nombreArchivo).Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar XML firmado");
                throw new InvalidOperationException(
                    $"Error al guardar XML firmado en archivo: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<(string XmlContent, string RutaArchivo)> GenerarYGuardarXmlAsync(int facturaId)
        {
            try
            {
                // Generar XML
                var xmlContent = await GenerarXmlFacturaAsync(facturaId);

                // Obtener clave de acceso
                var factura = await _context.Facturas.FindAsync(facturaId);

                if (factura == null)
                {
                    throw new InvalidOperationException($"No se encontró la factura con ID {facturaId}");
                }

                if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
                {
                    throw new InvalidOperationException($"La factura con ID {facturaId} no tiene clave de acceso generada");
                }

                // Guardar archivo
                var rutaArchivo = await GuardarXmlEnArchivoAsync(xmlContent, factura.ClaveAcceso);

                // Actualizar la ruta en la BD
                factura.XmlPath = rutaArchivo;
                await _context.SaveChangesAsync();

                _logger.LogInformation("XML generado y guardado para factura {FacturaId}. Ruta: {Ruta}",
                    facturaId, rutaArchivo);

                return (xmlContent, rutaArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar y guardar XML para factura {FacturaId}", facturaId);
                throw;
            }
        }
    }
}