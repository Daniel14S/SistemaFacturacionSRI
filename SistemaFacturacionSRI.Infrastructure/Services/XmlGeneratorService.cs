// SistemaFacturacionSRI.Infrastructure/Services/XmlGeneratorService.cs
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Text;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Models.XML;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    public class XmlGeneratorService : IXmlGeneratorService
    {
        private readonly string _xsdBasePath;
        private readonly string _xmlOutputPath;

        public XmlGeneratorService()
        {
            _xsdBasePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "XSD"
            );

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
            }
        }

        /// <summary>
        /// Genera el XML de una factura según el estándar SRI v1.1.0
        /// </summary>
        public string GenerarXmlFactura(FacturaDto factura)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(factura);
                if (factura.Detalles == null || !factura.Detalles.Any())
                {
                    throw new InvalidOperationException("La factura debe tener al menos un detalle para generar el XML.");
                }

                var (establecimiento, puntoEmision, secuencial) = ObtenerDatosEmision(factura.NumeroFactura);
                var cliente = factura.Cliente ?? new ClienteFacturaDto
                {
                    TipoIdentificacion = "07",
                    Identificacion = "9999999999999",
                    RazonSocial = "CONSUMIDOR FINAL",
                    Direccion = "NO DEFINIDA"
                };

                var fechaEmision = factura.FechaEmision == default
                    ? DateTime.Now
                    : factura.FechaEmision;

                var totalSinImpuestos = factura.Detalles.Sum(d => d.PrecioTotalSinImpuesto);
                var totalDescuento = factura.Detalles.Sum(d => d.Descuento);
                var totalFactura = factura.Total > 0 ? factura.Total : totalSinImpuestos - totalDescuento + factura.TotalIVA;

                // CORRECCIÓN CRÍTICA: Agregar el namespace del SRI
                XNamespace ns = "";
                
                var xml = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(ns + "factura",
                        new XAttribute("id", "comprobante"),
                        new XAttribute("version", "1.1.0"),
                        ConstruirInfoTributaria(ns, factura.ClaveAcceso, establecimiento, puntoEmision, secuencial),
                        ConstruirInfoFactura(ns, fechaEmision, cliente, totalSinImpuestos, totalDescuento, totalFactura, factura.Detalles),
                        ConstruirDetalles(ns, factura.Detalles),
                        ConstruirInfoAdicional(ns, factura.InfoAdicional)
                    )
                );

                // Retornar XML sin saltos de línea extra
                return xml.Declaration + Environment.NewLine + xml.ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al generar XML: {ex.Message}", ex);
            }
        }

        private static XElement ConstruirInfoTributaria(XNamespace ns, string? claveAcceso, string estab, string ptoEmi, string secuencial)
        {
            var clave = string.IsNullOrWhiteSpace(claveAcceso)
                ? new string('0', 49)
                : claveAcceso;

            return new XElement(ns + "infoTributaria",
                new XElement(ns + "ambiente", "1"),
                new XElement(ns + "tipoEmision", "1"),
                new XElement(ns + "razonSocial", "EMPRESA DE PRUEBA S.A."),
                new XElement(ns + "nombreComercial", "EMPRESA DE PRUEBA"),
                new XElement(ns + "ruc", "1234567890001"),
                new XElement(ns + "claveAcceso", clave),
                new XElement(ns + "codDoc", "01"),
                new XElement(ns + "estab", estab),
                new XElement(ns + "ptoEmi", ptoEmi),
                new XElement(ns + "secuencial", secuencial), // CORRECCIÓN: Mantener ceros a la izquierda
                new XElement(ns + "dirMatriz", "AV. PRINCIPAL 123")
            );
        }

        private static XElement ConstruirInfoFactura(
            XNamespace ns,
            DateTime fechaEmision,
            ClienteFacturaDto cliente,
            decimal totalSinImpuestos,
            decimal totalDescuento,
            decimal totalFactura,
            IEnumerable<DetalleFacturaDto> detalles)
        {
            var totalesImpuestos = ConstruirTotalesImpuestos(ns, detalles);

            return new XElement(ns + "infoFactura",
                new XElement(ns + "fechaEmision", fechaEmision.ToString("dd/MM/yyyy")),
                new XElement(ns + "dirEstablecimiento", "AV. PRINCIPAL 123"),
                new XElement(ns + "contribuyenteEspecial", "000"),
                new XElement(ns + "obligadoContabilidad", "SI"),
                new XElement(ns + "tipoIdentificacionComprador", cliente.TipoIdentificacion),
                new XElement(ns + "razonSocialComprador", cliente.RazonSocial),
                new XElement(ns + "identificacionComprador", cliente.Identificacion),
                new XElement(ns + "direccionComprador", cliente.Direccion ?? "NO DEFINIDA"),
                new XElement(ns + "totalSinImpuestos", FormatearDecimal(totalSinImpuestos)),
                new XElement(ns + "totalDescuento", FormatearDecimal(totalDescuento)),
                new XElement(ns + "totalConImpuestos", totalesImpuestos),
                new XElement(ns + "propina", FormatearDecimal(0)),
                new XElement(ns + "importeTotal", FormatearDecimal(totalFactura)),
                new XElement(ns + "moneda", "DOLAR"),
                new XElement(ns + "pagos",
                    new XElement(ns + "pago",
                        new XElement(ns + "formaPago", "01"),
                        new XElement(ns + "total", FormatearDecimal(totalFactura))
                    )
                )
            );
        }

        private static IEnumerable<XElement> ConstruirTotalesImpuestos(XNamespace ns, IEnumerable<DetalleFacturaDto> detalles)
        {
            var grupos = detalles
                .GroupBy(d => d.CodigoPorcentajeIVA)
                .Select(g => new
                {
                    CodigoPorcentaje = g.Key,
                    Base = g.Sum(x => x.BaseImponible),
                    Valor = g.Sum(x => x.Valor)
                })
                .ToList();

            if (!grupos.Any())
            {
                yield return new XElement(ns + "totalImpuesto",
                    new XElement(ns + "codigo", "2"),
                    new XElement(ns + "codigoPorcentaje", "0"),
                    new XElement(ns + "baseImponible", FormatearDecimal(0)),
                    new XElement(ns + "valor", FormatearDecimal(0))
                );
            }

            foreach (var impuesto in grupos)
            {
                yield return new XElement(ns + "totalImpuesto",
                    new XElement(ns + "codigo", "2"),
                    new XElement(ns + "codigoPorcentaje", impuesto.CodigoPorcentaje),
                    new XElement(ns + "baseImponible", FormatearDecimal(impuesto.Base)),
                    new XElement(ns + "valor", FormatearDecimal(impuesto.Valor))
                );
            }
        }

        private static XElement ConstruirDetalles(XNamespace ns, IEnumerable<DetalleFacturaDto> detalles)
        {
            return new XElement(ns + "detalles",
                detalles.Select(detalle =>
                    new XElement(ns + "detalle",
                        new XElement(ns + "codigoPrincipal", detalle.CodigoPrincipal),
                        detalle.CodigoAuxiliar != null
                            ? new XElement(ns + "codigoAuxiliar", detalle.CodigoAuxiliar)
                            : null,
                        new XElement(ns + "descripcion", detalle.Descripcion),
                        new XElement(ns + "cantidad", FormatearCantidad(detalle.Cantidad)),
                        new XElement(ns + "precioUnitario", FormatearDecimal(detalle.PrecioUnitario)),
                        new XElement(ns + "descuento", FormatearDecimal(detalle.Descuento)),
                        new XElement(ns + "precioTotalSinImpuesto", FormatearDecimal(detalle.PrecioTotalSinImpuesto)),
                        new XElement(ns + "impuestos",
                            new XElement(ns + "impuesto",
                                new XElement(ns + "codigo", "2"),
                                new XElement(ns + "codigoPorcentaje", detalle.CodigoPorcentajeIVA),
                                new XElement(ns + "tarifa", FormatearDecimal(detalle.Tarifa)),
                                new XElement(ns + "baseImponible", FormatearDecimal(detalle.BaseImponible)),
                                new XElement(ns + "valor", FormatearDecimal(detalle.Valor))
                            )
                        )
                    )
                )
            );
        }

        private static XElement? ConstruirInfoAdicional(XNamespace ns, List<InfoAdicionalDto>? infoAdicional)
        {
            if (infoAdicional == null || !infoAdicional.Any())
            {
                return null;
            }

            return new XElement(ns + "infoAdicional",
                infoAdicional.Select(campo =>
                    new XElement(ns + "campoAdicional",
                        new XAttribute("nombre", campo.Nombre),
                        campo.Valor))
            );
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

        private static string FormatearDecimal(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

        private static string FormatearCantidad(decimal valor) => valor.ToString("0.000000", CultureInfo.InvariantCulture);

        /// <summary>
        /// Valida un XML contra el esquema XSD del SRI
        /// </summary>
        public async Task<ResultadoValidacion> ValidarXmlContraEsquema(
            string xmlContent,
            string? xsdPath = null)
        {
            var errores = new List<ErrorValidacion>();

            try
            {
                xsdPath ??= Path.Combine(_xsdBasePath, "factura_v1.1.0.xsd");

                if (!File.Exists(xsdPath))
                {
                    return ResultadoValidacion.ConErrores(new List<ErrorValidacion>
                    {
                        new ErrorValidacion
                        {
                            Linea = 0,
                            Posicion = 0,
                            Mensaje = $"No se encontró el esquema XSD en: {xsdPath}. " +
                                      $"Descargue el esquema desde el portal del SRI y colóquelo en la carpeta Resources/XSD",
                            Severidad = "Error"
                        }
                    });
                }

                var resolver = new XmlUrlResolver();
                var schemas = new XmlSchemaSet
                {
                    XmlResolver = resolver
                };
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
                        Severidad = args.Severity == XmlSeverityType.Error
                            ? "Error"
                            : "Advertencia"
                    });
                };

                using (var stringReader = new StringReader(xmlContent))
                using (var reader = XmlReader.Create(stringReader, settings))
                {
                    while (await reader.ReadAsync())
                    {
                        // La validación ocurre automáticamente
                    }
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
            catch (XmlException ex)
            {
                return ResultadoValidacion.ConErrores(new List<ErrorValidacion>
                {
                    new ErrorValidacion
                    {
                        Linea = ex.LineNumber,
                        Posicion = ex.LinePosition,
                        Mensaje = $"Error de formato XML: {ex.Message}",
                        Severidad = "Error"
                    }
                });
            }
            catch (Exception ex)
            {
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

        /// <summary>
        /// Genera y valida el XML en un solo paso
        /// </summary>
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

                if (resultado.Errores.Any())
                {
                    Console.WriteLine($"Advertencias de validación XML: {resultado.Mensaje}");
                    foreach (var error in resultado.Errores)
                    {
                        Console.WriteLine($"  - {error}");
                    }
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

        /// <summary>
        /// Guarda el XML en un archivo en el servidor
        /// </summary>
        public async Task<string> GuardarXmlEnArchivo(string xmlContent, string claveAcceso)
        {
            try
            {
                var nombreArchivo = $"{claveAcceso}.xml";
                var rutaCompleta = Path.Combine(_xmlOutputPath, nombreArchivo);

                await File.WriteAllTextAsync(rutaCompleta, xmlContent, Encoding.UTF8);

                return Path.Combine("comprobantes", "xml", nombreArchivo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al guardar XML en archivo: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// T-064: Guarda el XML firmado en el sistema de archivos
        /// </summary>
        public async Task<string> GuardarXmlFirmadoEnArchivo(string xmlFirmado, string claveAcceso)
        {
            try
            {
                // Crear directorio para XMLs firmados si no existe
                var directorioFirmados = Path.Combine(_xmlOutputPath, "firmados");
                if (!Directory.Exists(directorioFirmados))
                {
                    Directory.CreateDirectory(directorioFirmados);
                }

                var nombreArchivo = $"{claveAcceso}_firmado.xml";
                var rutaCompleta = Path.Combine(directorioFirmados, nombreArchivo);

                await File.WriteAllTextAsync(rutaCompleta, xmlFirmado, Encoding.UTF8);

                return Path.Combine("comprobantes", "xml", "firmados", nombreArchivo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al guardar XML firmado en archivo: {ex.Message}",
                    ex
                );
            }
        }

        // ============================================================
        // MÉTODOS ANTERIORES (STUB - IMPLEMENTAR DESPUÉS)
        // ============================================================

        public async Task<string> GenerarXmlFacturaAsync(int facturaId)
        {
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }

        public async Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId)
        {
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }

        public async Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent)
        {
            var resultado = await ValidarXmlContraEsquema(xmlContent);
            var errores = resultado.Errores.Select(e => e.ToString()).ToList();
            return (resultado.EsValido, errores);
        }

        public async Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso)
        {
            return await GuardarXmlEnArchivo(xmlContent, claveAcceso);
        }

        public string SerializarFacturaXml(FacturaXML facturaXml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(FacturaXML));
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                });

                serializer.Serialize(xmlWriter, facturaXml);
                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al serializar XML: {ex.Message}", ex);
            }
        }

        public FacturaXML DeserializarFacturaXml(string xmlContent)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(FacturaXML));
                using var stringReader = new StringReader(xmlContent);
                return (FacturaXML)serializer.Deserialize(stringReader)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al deserializar XML: {ex.Message}", ex);
            }
        }

        public async Task<(string XmlContent, string RutaArchivo)> GenerarYGuardarXmlAsync(int facturaId)
        {
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }
    }
}