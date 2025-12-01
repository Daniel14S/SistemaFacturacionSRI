// SistemaFacturacionSRI.Infrastructure/Services/XmlGeneratorService.cs
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
            // Ruta a los esquemas XSD
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

            // Crear directorios si no existen
            if (!Directory.Exists(_xsdBasePath))
            {
                Directory.CreateDirectory(_xsdBasePath);
            }

            if (!Directory.Exists(_xmlOutputPath))
            {
                Directory.CreateDirectory(_xmlOutputPath);
            }
        }

        // ============================================================
        // MÉTODOS NUEVOS PARA T-046
        // ============================================================

        /// <summary>
        /// Genera el XML de una factura según el estándar SRI v1.1.0
        /// </summary>
        public string GenerarXmlFactura(FacturaDto factura)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(factura);
                var claveAcceso = factura.ClaveAcceso ?? string.Empty;
                // TODO: Implementar generación completa del XML
                // Por ahora retornamos un XML básico de ejemplo
                var xml = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement("factura",
                        new XAttribute("id", "comprobante"),
                        new XAttribute("version", "1.1.0"),
                        new XElement("infoTributaria",
                            new XElement("ambiente", "1"),
                            new XElement("tipoEmision", "1"),
                            new XElement("razonSocial", "EMPRESA PRUEBA"),
                            new XElement("ruc", "1234567890001"),
                            new XElement("claveAcceso", claveAcceso),
                            new XElement("codDoc", "01"),
                            new XElement("estab", "001"),
                            new XElement("ptoEmi", "001"),
                            new XElement("secuencial", "000000001"),
                            new XElement("dirMatriz", "DIRECCION MATRIZ")
                        )
                    )
                );

                return xml.Declaration.ToString() + Environment.NewLine + xml.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al generar XML: {ex.Message}", ex);
            }
        }

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

                var schemas = new XmlSchemaSet();
                schemas.Add("", xsdPath);

                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    Schemas = schemas,
                    ValidationFlags =
                        XmlSchemaValidationFlags.ReportValidationWarnings |
                        XmlSchemaValidationFlags.ProcessSchemaLocation |
                        XmlSchemaValidationFlags.ProcessInlineSchema,
                    Async = true
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

        // ============================================================
        // MÉTODOS ANTERIORES (STUB - IMPLEMENTAR DESPUÉS)
        // ============================================================

        public async Task<string> GenerarXmlFacturaAsync(int facturaId)
        {
            // TODO: Implementar en tareas posteriores
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }

        public async Task<FacturaXML> GenerarObjetoFacturaXmlAsync(int facturaId)
        {
            // TODO: Implementar en tareas posteriores
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }

        public async Task<(bool EsValido, List<string> Errores)> ValidarXmlContraEsquemaAsync(string xmlContent)
        {
            // Usar el nuevo método de validación
            var resultado = await ValidarXmlContraEsquema(xmlContent);
            var errores = resultado.Errores.Select(e => e.ToString()).ToList();
            return (resultado.EsValido, errores);
        }

        public async Task<string> GuardarXmlEnArchivoAsync(string xmlContent, string claveAcceso)
        {
            // Usar el nuevo método
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
            // TODO: Implementar en tareas posteriores
            throw new NotImplementedException("Método pendiente de implementación en tareas futuras");
        }
    }
}