// SistemaFacturacionSRI.Infrastructure/Services/SRI/SoapResponseParser.cs
// T-074: Parser para respuestas SOAP XML del SRI

using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.DTOs.SRI;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-074: Parser de respuestas SOAP XML del SRI
    /// </summary>
    public class SoapResponseParser
    {
        private readonly ILogger<SoapResponseParser> _logger;

        // Namespaces del SRI
        private const string NS_SOAP = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_RECEPCION = "http://ec.gob.sri.ws.recepcion";
        private const string NS_AUTORIZACION = "http://ec.gob.sri.ws.autorizacion";

        public SoapResponseParser(ILogger<SoapResponseParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================================
        // T-074: PARSEO DE RESPUESTA DE RECEPCIÓN
        // ============================================================

        /// <summary>
        /// T-074: Parsea la respuesta SOAP de recepción de comprobantes
        /// </summary>
        public RespuestaRecepcionComprobante ParsearRespuestaRecepcion(string soapXml)
        {
            try
            {
                _logger.LogDebug("Parseando respuesta de recepción...");

                var doc = XDocument.Parse(soapXml);
                var ns = new XmlNamespaceManager(new NameTable());
                ns.AddNamespace("soap", NS_SOAP);
                ns.AddNamespace("ns2", NS_RECEPCION);

                // Buscar el nodo RespuestaRecepcionComprobante
                var respuestaNode = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "RespuestaRecepcionComprobante");

                if (respuestaNode == null)
                {
                    throw new InvalidOperationException("No se encontró RespuestaRecepcionComprobante en el XML");
                }

                var respuesta = new RespuestaRecepcionComprobante
                {
                    Estado = ObtenerValorElemento(respuestaNode, "estado") ?? "DESCONOCIDO"
                };

                // Parsear comprobantes
                var comprobantesNode = respuestaNode.Element("comprobantes");
                if (comprobantesNode != null)
                {
                    foreach (var comprobanteNode in comprobantesNode.Elements("comprobante"))
                    {
                        var comprobante = ParsearComprobanteRecibido(comprobanteNode);
                        respuesta.Comprobantes.Add(comprobante);
                    }
                }

                _logger.LogDebug("Respuesta de recepción parseada: Estado={Estado}, Comprobantes={Count}",
                    respuesta.Estado, respuesta.Comprobantes.Count);

                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando respuesta de recepción");
                throw new InvalidOperationException("Error al parsear respuesta de recepción del SRI", ex);
            }
        }

        /// <summary>
        /// T-074: Parsea un comprobante recibido
        /// </summary>
        private ComprobanteRecibido ParsearComprobanteRecibido(XElement comprobanteNode)
        {
            var comprobante = new ComprobanteRecibido
            {
                ClaveAcceso = ObtenerValorElemento(comprobanteNode, "claveAcceso") ?? string.Empty
            };

            // Parsear mensajes
            var mensajesNode = comprobanteNode.Element("mensajes");
            if (mensajesNode != null)
            {
                foreach (var mensajeNode in mensajesNode.Elements("mensaje"))
                {
                    var mensaje = ParsearMensaje(mensajeNode);
                    comprobante.Mensajes.Add(mensaje);
                }
            }

            return comprobante;
        }

        // ============================================================
        // T-074: PARSEO DE RESPUESTA DE AUTORIZACIÓN
        // ============================================================

        /// <summary>
        /// T-074: Parsea la respuesta SOAP de autorización de comprobantes
        /// </summary>
        public RespuestaAutorizacionComprobante ParsearRespuestaAutorizacion(string soapXml)
        {
            try
            {
                _logger.LogDebug("Parseando respuesta de autorización...");

                var doc = XDocument.Parse(soapXml);

                // Buscar el nodo RespuestaAutorizacionComprobante
                var respuestaNode = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "RespuestaAutorizacionComprobante");

                if (respuestaNode == null)
                {
                    throw new InvalidOperationException("No se encontró RespuestaAutorizacionComprobante en el XML");
                }

                var respuesta = new RespuestaAutorizacionComprobante
                {
                    ClaveAccesoConsultada = ObtenerValorElemento(respuestaNode, "claveAccesoConsultada") ?? string.Empty,
                    NumeroComprobantes = int.TryParse(
                        ObtenerValorElemento(respuestaNode, "numeroComprobantes"),
                        out var num) ? num : 0
                };

                // Parsear autorizaciones
                var autorizacionesNode = respuestaNode.Element("autorizaciones");
                if (autorizacionesNode != null)
                {
                    foreach (var autorizacionNode in autorizacionesNode.Elements("autorizacion"))
                    {
                        var autorizacion = ParsearAutorizacion(autorizacionNode);
                        respuesta.Autorizaciones.Add(autorizacion);
                    }
                }

                _logger.LogDebug("Respuesta de autorización parseada: ClaveAcceso={Clave}, Autorizaciones={Count}",
                    respuesta.ClaveAccesoConsultada, respuesta.Autorizaciones.Count);

                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando respuesta de autorización");
                throw new InvalidOperationException("Error al parsear respuesta de autorización del SRI", ex);
            }
        }

        /// <summary>
        /// T-074: Parsea una autorización
        /// </summary>
        private Autorizacion ParsearAutorizacion(XElement autorizacionNode)
        {
            var autorizacion = new Autorizacion
            {
                Estado = ObtenerValorElemento(autorizacionNode, "estado") ?? "DESCONOCIDO",
                NumeroAutorizacion = ObtenerValorElemento(autorizacionNode, "numeroAutorizacion") ?? string.Empty,
                FechaAutorizacion = ObtenerValorElemento(autorizacionNode, "fechaAutorizacion") ?? string.Empty,
                Ambiente = ObtenerValorElemento(autorizacionNode, "ambiente") ?? string.Empty,
                Comprobante = ExtraerCData(autorizacionNode, "comprobante") ?? string.Empty
            };

            // Parsear fecha
            autorizacion.ParsearFechaAutorizacion();

            // Parsear mensajes
            var mensajesNode = autorizacionNode.Element("mensajes");
            if (mensajesNode != null)
            {
                foreach (var mensajeNode in mensajesNode.Elements("mensaje"))
                {
                    var mensaje = ParsearMensaje(mensajeNode);
                    autorizacion.Mensajes.Add(mensaje);
                }
            }

            return autorizacion;
        }

        // ============================================================
        // T-074: PARSEO DE MENSAJES
        // ============================================================

        /// <summary>
        /// T-074: Parsea un mensaje del SRI
        /// </summary>
        private MensajeSri ParsearMensaje(XElement mensajeNode)
        {
            return new MensajeSri
            {
                Identificador = ObtenerValorElemento(mensajeNode, "identificador") ?? string.Empty,
                Mensaje = ObtenerValorElemento(mensajeNode, "mensaje") ?? string.Empty,
                InformacionAdicional = ObtenerValorElemento(mensajeNode, "informacionAdicional"),
                Tipo = ObtenerValorElemento(mensajeNode, "tipo") ?? "DESCONOCIDO"
            };
        }

        // ============================================================
        // T-074: MÉTODOS AUXILIARES
        // ============================================================

        /// <summary>
        /// Obtiene el valor de un elemento hijo
        /// </summary>
        private string? ObtenerValorElemento(XElement parent, string elementName)
        {
            var elemento = parent.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
            
            return elemento?.Value;
        }

        /// <summary>
        /// Extrae contenido CDATA de un elemento
        /// </summary>
        private string? ExtraerCData(XElement parent, string elementName)
        {
            var elemento = parent.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));

            if (elemento == null)
            {
                return null;
            }

            // Buscar CDATA
            var cdata = elemento.Nodes().OfType<XCData>().FirstOrDefault();
            if (cdata != null)
            {
                return cdata.Value;
            }

            // Si no hay CDATA, retornar el valor directo
            return elemento.Value;
        }

        /// <summary>
        /// T-074: Valida que el XML sea una respuesta SOAP válida
        /// </summary>
        public bool EsRespuestaSoapValida(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                
                // Buscar nodo Envelope
                var envelope = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Envelope");

                if (envelope == null)
                {
                    return false;
                }

                // Buscar nodo Body
                var body = envelope.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Body");

                return body != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// T-074: Detecta si la respuesta es de recepción o autorización
        /// </summary>
        public TipoRespuestaSri DetectarTipoRespuesta(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);

                if (doc.Descendants().Any(e => e.Name.LocalName == "RespuestaRecepcionComprobante"))
                {
                    return TipoRespuestaSri.Recepcion;
                }

                if (doc.Descendants().Any(e => e.Name.LocalName == "RespuestaAutorizacionComprobante"))
                {
                    return TipoRespuestaSri.Autorizacion;
                }

                return TipoRespuestaSri.Desconocido;
            }
            catch
            {
                return TipoRespuestaSri.Desconocido;
            }
        }

        /// <summary>
        /// T-074: Extrae el mensaje de error de un SOAP Fault
        /// </summary>
        public string? ExtraerMensajeErrorSoapFault(string soapXml)
        {
            try
            {
                var doc = XDocument.Parse(soapXml);

                // Buscar nodo Fault
                var fault = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Fault");

                if (fault == null)
                {
                    return null;
                }

                // Buscar faultstring
                var faultString = fault.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "faultstring");

                return faultString?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// T-074: Log de respuesta SOAP para debugging
        /// </summary>
        public void LogRespuestaSoap(string soapXml, string operacion)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            try
            {
                // Formatear XML para mejor legibilidad
                var doc = XDocument.Parse(soapXml);
                var xmlFormateado = doc.ToString();

                _logger.LogDebug("═══════════════════════════════════════");
                _logger.LogDebug("RESPUESTA SOAP - {Operacion}", operacion);
                _logger.LogDebug("═══════════════════════════════════════");
                _logger.LogDebug("{Xml}", xmlFormateado);
                _logger.LogDebug("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo formatear XML para logging");
                _logger.LogDebug("Respuesta SOAP (sin formatear): {Xml}", soapXml);
            }
        }
    }

    /// <summary>
    /// T-074: Tipo de respuesta SOAP del SRI
    /// </summary>
    public enum TipoRespuestaSri
    {
        Desconocido = 0,
        Recepcion = 1,
        Autorizacion = 2
    }
}