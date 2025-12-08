// SistemaFacturacionSRI.Infrastructure/Services/SRI/SoapResponseParser.cs
// T-074: Parser para respuestas SOAP XML del SRI
// ✅ CORREGIDO: Manejo correcto del estado de recepción

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
        // T-074: PARSEO DE RESPUESTA DE RECEPCIÓN - ✅ CORREGIDO
        // ============================================================

        /// <summary>
        /// T-074: Parsea la respuesta SOAP de recepción de comprobantes
        /// ✅ CORREGIDO: El estado viene en cada comprobante, no en el nodo raíz
        /// </summary>
        // SoapResponseParser.cs - VERSIÓN MEJORADA CON DIAGNÓSTICO COMPLETO

        /// <summary>
        /// Parsea la respuesta SOAP de recepción con logging detallado
        /// </summary>
        public RespuestaRecepcionComprobante ParsearRespuestaRecepcion(string soapXml)
        {
            try
            {
                _logger.LogDebug("\n═══════════════════════════════════════════════════════");
                _logger.LogDebug("PARSEANDO RESPUESTA DE RECEPCIÓN");
                _logger.LogDebug("═══════════════════════════════════════════════════════");
                _logger.LogDebug("Tamaño respuesta: {Size} bytes", soapXml.Length);

                // Log del XML completo si está en modo TRACE
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("\n[XML RESPUESTA COMPLETO]");
                    _logger.LogTrace("{Xml}", soapXml);
                }

                var doc = XDocument.Parse(soapXml);

                // Buscar el nodo RespuestaRecepcionComprobante
                var respuestaNode = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "RespuestaRecepcionComprobante");

                if (respuestaNode == null)
                {
<<<<<<< HEAD
                    _logger.LogError("❌ No se encontró nodo RespuestaRecepcionComprobante");
                    _logger.LogDebug("XML recibido:\n{Xml}", soapXml);
                    throw new InvalidOperationException("No se encontró RespuestaRecepcionComprobante en el XML");
                }

                var respuesta = new RespuestaRecepcionComprobante();
=======
                    _logger.LogWarning("⚠️ No se encontró 'RespuestaRecepcionComprobante'");
                    _logger.LogWarning("Elementos encontrados en el XML:");

                    LogEstructuraXml(doc);

                    throw new InvalidOperationException(
                        "No se encontró RespuestaRecepcionComprobante en el XML. " +
                        "El SRI puede haber devuelto un error o formato inesperado.");
                }

                // Extraer estado
                var estadoRaw = ObtenerValorElemento(respuestaNode, "estado");
                _logger.LogDebug("Estado raw del XML: '{Estado}'", estadoRaw ?? "null");

                var estadoNormalizado = (estadoRaw?.Trim().ToUpperInvariant()) ?? "DESCONOCIDO";
                _logger.LogDebug("Estado normalizado: '{Estado}'", estadoNormalizado);

                var respuesta = new RespuestaRecepcionComprobante
                {
                    Estado = estadoNormalizado
                };
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6

                // ✅ FIX: Parsear comprobantes PRIMERO
                var comprobantesNode = respuestaNode.Element("comprobantes");

                if (comprobantesNode != null)
                {
                    _logger.LogDebug("Nodo 'comprobantes' encontrado");

                    var comprobantesEncontrados = comprobantesNode.Elements("comprobante").ToList();
                    _logger.LogDebug("Comprobantes en respuesta: {Count}", comprobantesEncontrados.Count);

                    foreach (var comprobanteNode in comprobantesEncontrados)
                    {
                        var comprobante = ParsearComprobanteRecibido(comprobanteNode);
                        respuesta.Comprobantes.Add(comprobante);
<<<<<<< HEAD
                        
                        _logger.LogDebug("Comprobante parseado: ClaveAcceso={Clave}, Mensajes={Count}",
                            comprobante.ClaveAcceso, comprobante.Mensajes.Count);
                    }
                }

                // ✅ FIX: El estado se deriva del primer comprobante
                if (respuesta.Comprobantes.Any())
                {
                    var primerComprobante = respuesta.Comprobantes.First();
                    
                    // Si tiene mensajes de error (identificador 43 o tipo ERROR)
                    var tieneErrores = primerComprobante.Mensajes.Any(m => 
                        m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                        m.Identificador.StartsWith("4")); // Códigos 4x son errores
                    
                    respuesta.Estado = tieneErrores ? "DEVUELTA" : "RECIBIDA";
                    
                    _logger.LogDebug("Estado determinado: {Estado} (basado en {Count} mensajes, tieneErrores={TieneErrores})",
                        respuesta.Estado, primerComprobante.Mensajes.Count, tieneErrores);
                }
                else
                {
                    // Si no hay comprobantes, es una respuesta extraña
                    respuesta.Estado = "DESCONOCIDO";
                    _logger.LogWarning("⚠️ Respuesta sin comprobantes - Estado: DESCONOCIDO");
                }

                _logger.LogDebug("Respuesta de recepción parseada: Estado={Estado}, Comprobantes={Count}",
                    respuesta.Estado, respuesta.Comprobantes.Count);
=======

                        _logger.LogDebug("\n  Comprobante parseado:");
                        _logger.LogDebug("    ClaveAcceso: {Clave}", comprobante.ClaveAcceso);
                        _logger.LogDebug("    Mensajes: {Count}", comprobante.Mensajes.Count);

                        foreach (var msg in comprobante.Mensajes)
                        {
                            _logger.LogDebug("      [{Id}] {Tipo}: {Mensaje}",
                                msg.Identificador, msg.Tipo, msg.Mensaje);

                            if (!string.IsNullOrEmpty(msg.InformacionAdicional))
                            {
                                _logger.LogDebug("        Info: {Info}", msg.InformacionAdicional);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("⚠️ No se encontró nodo 'comprobantes'");
                }

                // ✨ LÓGICA MEJORADA: Si el estado es DESCONOCIDO pero hay mensajes de ERROR
                if (respuesta.Estado == "DESCONOCIDO")
                {
                    var hayErrores = respuesta.Comprobantes
                        .SelectMany(c => c.Mensajes)
                        .Any(m => m.Tipo?.ToUpperInvariant() == "ERROR");

                    if (hayErrores)
                    {
                        _logger.LogWarning("⚠️ Estado era DESCONOCIDO pero hay mensajes de ERROR");
                        _logger.LogWarning("   Cambiando estado a DEVUELTA");
                        respuesta.Estado = "DEVUELTA";
                    }
                }

                _logger.LogDebug("\n[RESULTADO FINAL]");
                _logger.LogDebug("  Estado: {Estado}", respuesta.Estado);
                _logger.LogDebug("  Comprobantes: {Count}", respuesta.Comprobantes.Count);
                _logger.LogDebug("  FueRecibido: {Recibido}", respuesta.FueRecibido);
                _logger.LogDebug("  FueDevuelto: {Devuelto}", respuesta.FueDevuelto);
                _logger.LogDebug("═══════════════════════════════════════════════════════\n");
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6

                return respuesta;
            }
            catch (XmlException ex)
            {
                _logger.LogError("\n❌ ERROR AL PARSEAR XML");
                _logger.LogError("  Línea: {Line}, Posición: {Pos}", ex.LineNumber, ex.LinePosition);
                _logger.LogError("  Mensaje: {Mensaje}", ex.Message);

                _logger.LogError("\nXML problemático (primeros 1000 caracteres):");
                _logger.LogError("{Xml}", soapXml.Substring(0, Math.Min(1000, soapXml.Length)));

                // Mostrar la línea con error
                var lineas = soapXml.Split('\n');
                if (ex.LineNumber > 0 && ex.LineNumber <= lineas.Length)
                {
                    _logger.LogError("\nLínea con error:");
                    _logger.LogError("{Linea}", lineas[ex.LineNumber - 1]);

                    if (ex.LinePosition > 0)
                    {
                        var pointer = new string(' ', Math.Max(0, ex.LinePosition - 1)) + "^";
                        _logger.LogError("{Pointer}", pointer);
                    }
                }

                throw new InvalidOperationException(
                    $"Error al parsear respuesta XML del SRI en línea {ex.LineNumber}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "\n❌ ERROR INESPERADO al parsear respuesta");
                _logger.LogError("  Tipo: {Type}", ex.GetType().Name);
                _logger.LogError("  Mensaje: {Message}", ex.Message);

                throw new InvalidOperationException(
                    "Error al parsear respuesta de recepción del SRI", ex);
            }
        }

        /// <summary>
<<<<<<< HEAD
        /// T-074: Parsea un comprobante recibido
        /// ✅ MEJORADO: Incluye el estado del comprobante si existe
=======
        /// Log de la estructura del XML para debugging
        /// </summary>
        private void LogEstructuraXml(XDocument doc)
        {
            try
            {
                _logger.LogDebug("\n[ESTRUCTURA XML]");

                var elementos = doc.Descendants()
                    .Select(e => new {
                        Nombre = e.Name.LocalName,
                        Namespace = e.Name.NamespaceName
                    })
                    .GroupBy(e => e.Nombre)
                    .Select(g => g.First())
                    .ToList();

                foreach (var elemento in elementos)
                {
                    if (!string.IsNullOrEmpty(elemento.Namespace))
                    {
                        _logger.LogDebug("  • {Nombre} (xmlns: {Namespace})",
                            elemento.Nombre, elemento.Namespace);
                    }
                    else
                    {
                        _logger.LogDebug("  • {Nombre}", elemento.Nombre);
                    }
                }

                // Buscar si hay un Fault
                var fault = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Fault");

                if (fault != null)
                {
                    _logger.LogWarning("\n⚠️ Se encontró un SOAP Fault:");

                    var faultCode = fault.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value;
                    var faultString = fault.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
                    var faultDetail = fault.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "detail")?.Value;

                    _logger.LogWarning("  FaultCode: {Code}", faultCode ?? "N/A");
                    _logger.LogWarning("  FaultString: {String}", faultString ?? "N/A");

                    if (!string.IsNullOrEmpty(faultDetail))
                    {
                        _logger.LogWarning("  Detail: {Detail}", faultDetail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo analizar estructura XML");
            }
        }

        /// <summary>
        /// Parsea un comprobante recibido con logging detallado
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6
        /// </summary>
        private ComprobanteRecibido ParsearComprobanteRecibido(XElement comprobanteNode)
        {
            var claveAcceso = ObtenerValorElemento(comprobanteNode, "claveAcceso");

            _logger.LogTrace("Parseando comprobante:");
            _logger.LogTrace("  ClaveAcceso: {Clave}", claveAcceso ?? "null");

            var comprobante = new ComprobanteRecibido
            {
                ClaveAcceso = claveAcceso ?? string.Empty
            };

            // ✅ Intentar obtener estado del comprobante si existe
            var estadoComprobante = ObtenerValorElemento(comprobanteNode, "estado");
            if (!string.IsNullOrEmpty(estadoComprobante))
            {
                comprobante.Estado = estadoComprobante;
                _logger.LogDebug("Estado encontrado en comprobante: {Estado}", estadoComprobante);
            }

            // Parsear mensajes
            var mensajesNode = comprobanteNode.Element("mensajes");

            if (mensajesNode != null)
            {
                var mensajesEncontrados = mensajesNode.Elements("mensaje").ToList();
                _logger.LogTrace("  Mensajes encontrados: {Count}", mensajesEncontrados.Count);

                foreach (var mensajeNode in mensajesEncontrados)
                {
                    var mensaje = ParsearMensaje(mensajeNode);
                    comprobante.Mensajes.Add(mensaje);
<<<<<<< HEAD
                    
                    _logger.LogDebug("Mensaje parseado: [{Id}] {Tipo}: {Mensaje}",
=======

                    _logger.LogTrace("    Mensaje: [{Id}] {Tipo} - {Mensaje}",
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6
                        mensaje.Identificador, mensaje.Tipo, mensaje.Mensaje);
                }
            }
            else
            {
<<<<<<< HEAD
                _logger.LogDebug("⚠️ Comprobante sin mensajes");
=======
                _logger.LogTrace("  ⚠️ No se encontró nodo 'mensajes'");
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6
            }

            return comprobante;
        }

        /// <summary>
        /// Parsea un mensaje del SRI con validación
        /// </summary>
        private MensajeSri ParsearMensaje(XElement mensajeNode)
        {
            var identificador = ObtenerValorElemento(mensajeNode, "identificador")?.Trim() ?? "SIN_ID";
            var mensaje = ObtenerValorElemento(mensajeNode, "mensaje")?.Trim() ?? "Sin mensaje";
            var tipo = ObtenerValorElemento(mensajeNode, "tipo")?.Trim() ?? "DESCONOCIDO";
            var infoAdicional = ObtenerValorElemento(mensajeNode, "informacionAdicional")?.Trim();

            // Normalizar tipo
            tipo = tipo.ToUpperInvariant();

            _logger.LogTrace("      MensajeSRI parseado:");
            _logger.LogTrace("        ID: {Id}", identificador);
            _logger.LogTrace("        Tipo: {Tipo}", tipo);
            _logger.LogTrace("        Mensaje: {Msg}", mensaje);

            if (!string.IsNullOrEmpty(infoAdicional))
            {
                _logger.LogTrace("        Info adicional: {Info}", infoAdicional);
            }

            return new MensajeSri
            {
                Identificador = identificador,
                Mensaje = mensaje,
                InformacionAdicional = infoAdicional,
                Tipo = tipo
            };
        }

        /// <summary>
        /// T-074: Parsea un comprobante recibido
        /// </summary>
        
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
        // T-074: PARSEO DE MENSAJES - ✅ MEJORADO
        // ============================================================

        /// <summary>
        /// T-074: Parsea un mensaje del SRI
        /// ✅ MEJORADO: Mejor manejo de campos opcionales
        /// </summary>
<<<<<<< HEAD
        private MensajeSri ParsearMensaje(XElement mensajeNode)
        {
            var identificador = ObtenerValorElemento(mensajeNode, "identificador") ?? string.Empty;
            var mensaje = ObtenerValorElemento(mensajeNode, "mensaje") ?? string.Empty;
            var tipo = ObtenerValorElemento(mensajeNode, "tipo");
            
            // ✅ Si no hay tipo, inferirlo del identificador
            if (string.IsNullOrEmpty(tipo))
            {
                tipo = InferirTipoMensaje(identificador);
            }

            return new MensajeSri
            {
                Identificador = identificador,
                Mensaje = mensaje,
                InformacionAdicional = ObtenerValorElemento(mensajeNode, "informacionAdicional"),
                Tipo = tipo
            };
        }

        /// <summary>
        /// ✅ NUEVO: Infiere el tipo de mensaje según el identificador
        /// </summary>
        private string InferirTipoMensaje(string identificador)
        {
            if (string.IsNullOrEmpty(identificador))
            {
                return "DESCONOCIDO";
            }

            // Códigos que empiezan con 4 son errores
            if (identificador.StartsWith("4"))
            {
                return "ERROR";
            }

            // Códigos que empiezan con 3 son advertencias
            if (identificador.StartsWith("3"))
            {
                return "ADVERTENCIA";
            }

            // El resto son informativos
            return "INFORMATIVO";
        }

=======
        
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6
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
            
            return elemento?.Value?.Trim();
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
                if (string.IsNullOrWhiteSpace(xml))
                {
                    _logger.LogWarning("XML vacío o nulo");
                    return false;
                }

                var doc = XDocument.Parse(xml);
                
                // Buscar nodo Envelope
                var envelope = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Envelope");

                if (envelope == null)
                {
                    _logger.LogWarning("No se encontró nodo Envelope");
                    return false;
                }

                // Buscar nodo Body
                var body = envelope.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Body");

                if (body == null)
                {
                    _logger.LogWarning("No se encontró nodo Body");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validando XML SOAP");
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

        /// <summary>
        /// ✅ NUEVO: Método de debugging para analizar estructura del XML
        /// </summary>
        public void AnalizarEstructuraXml(string xml, string contexto)
        {
            try
            {
                _logger.LogDebug("═══════════════════════════════════════");
                _logger.LogDebug("ANÁLISIS DE ESTRUCTURA XML - {Contexto}", contexto);
                _logger.LogDebug("═══════════════════════════════════════");

                var doc = XDocument.Parse(xml);
                AnalizarNodo(doc.Root, 0);

                _logger.LogDebug("═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando estructura XML");
            }
        }

        private void AnalizarNodo(XElement? nodo, int nivel)
        {
            if (nodo == null) return;

            var indent = new string(' ', nivel * 2);
            var valor = string.IsNullOrWhiteSpace(nodo.Value) ? "" : $" = {nodo.Value.Substring(0, Math.Min(50, nodo.Value.Length))}";
            
            _logger.LogDebug("{Indent}<{Nombre}>{Valor}", indent, nodo.Name.LocalName, valor);

            foreach (var hijo in nodo.Elements())
            {
                AnalizarNodo(hijo, nivel + 1);
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