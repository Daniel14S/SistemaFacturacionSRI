// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriWebServiceClient.cs
// T-076: Construcción de requests SOAP
// T-077: Envío HTTP al SRI
// ✨ MEJORADO: Logging detallado y diagnóstico completo

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-076, T-077: Cliente para consumir WebServices SOAP del SRI
    /// ✨ Con logging detallado y diagnóstico completo
    /// </summary>
    public class SriWebServiceClient : ISriWebServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly SriWebServicesOptions _options;
        private readonly SoapResponseParser _parser;
        private readonly ILogger<SriWebServiceClient> _logger;

        public SriWebServiceClient(
            HttpClient httpClient,
            IOptions<SriWebServicesOptions> options,
            SoapResponseParser parser,
            ILogger<SriWebServiceClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.Validar();
            ConfigurarHttpClient();
        }

        // ============================================================
        // T-077: CONFIGURACIÓN HTTP CLIENT
        // ============================================================

        private void ConfigurarHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSegundos);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/xml, application/xml");

            _logger.LogInformation("╔══════════════════════════════════════════════════╗");
            _logger.LogInformation("║   CONFIGURACIÓN HTTP CLIENT                      ║");
            _logger.LogInformation("╚══════════════════════════════════════════════════╝");
            _logger.LogInformation("  → Timeout: {Timeout}s", _options.TimeoutSegundos);
            _logger.LogInformation("  → User-Agent: {UserAgent}", _options.UserAgent);
        }

        // ============================================================
        // T-076, T-077: ENVIAR COMPROBANTE (RECEPCIÓN)
        // ============================================================

        // ============================================================
        // T-076, T-077: ENVIAR COMPROBANTE (RECEPCIÓN)
        // ============================================================

        // ============================================================
        // T-076, T-077: ENVIAR COMPROBANTE (RECEPCIÓN)
        // ✨ CON DECODIFICACIÓN Y DIAGNÓSTICO COMPLETO DEL XML
        // ============================================================

        public async Task<RespuestaRecepcionComprobante> EnviarComprobanteAsync(
            RecepcionComprobanteRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║       ENVIANDO COMPROBANTE AL SRI                ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  → Clave Acceso: {ClaveAcceso}", request.ClaveAcceso);
                _logger.LogInformation("  → URL: {Url}", _options.UrlRecepcion);
                _logger.LogInformation("  → Ambiente: {Ambiente}", _options.UrlRecepcion.Contains("celcer") ? "PRUEBAS" : "PRODUCCIÓN");

                // Validar request
                request.Validar();

                // T-076: Construir SOAP request
                var soapRequest = request.GenerarSoapXml();

                _logger.LogInformation("  → Tamaño SOAP: {Size} bytes", soapRequest.Length);

                // ✨ DECODIFICAR Y MOSTRAR EL XML REAL DEL COMPROBANTE
                _logger.LogInformation("\n[DECODIFICANDO XML] Intentando extraer y decodificar el XML del SOAP...");
                try
                {
                    var xmlDecodificado = DecodificarXmlDesdeSoap(soapRequest);
                    if (!string.IsNullOrEmpty(xmlDecodificado))
                    {
                        _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                        _logger.LogInformation("║   📄 XML REAL DEL COMPROBANTE (DECODIFICADO)    ║");
                        _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                        _logger.LogInformation("\n{XmlDecodificado}\n", xmlDecodificado);

                        // Mostrar información resumida
                        MostrarResumenXml(xmlDecodificado);
                    }
                    else
                    {
                        _logger.LogWarning("  ⚠️ El XML decodificado está vacío");
                    }
                }
                catch (Exception exDecode)
                {
                    _logger.LogError("  ❌ Error al decodificar el XML: {Error}", exDecode.Message);
                    _logger.LogError("  → Stack Trace: {StackTrace}", exDecode.StackTrace);
                }

                _logger.LogInformation("\n[SOAP REQUEST] Enviando el siguiente SOAP envelope:");
                _logger.LogInformation("  → Primeros 500 caracteres del SOAP:");
                _logger.LogInformation("  {SoapRequest}", soapRequest.Substring(0, Math.Min(500, soapRequest.Length)));

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("\n[SOAP REQUEST COMPLETO]");
                    _logger.LogDebug("{Soap}", soapRequest);
                }

                // T-077: Enviar HTTP POST
                _logger.LogInformation("\n[HTTP] Enviando request...");
                var responseXml = await EnviarSoapRequestAsync(
                    _options.UrlRecepcion,
                    soapRequest,
                    "Recepcion",
                    cancellationToken);

                if (_options.LogSoapDetallado)
                {
                    _parser.LogRespuestaSoap(responseXml, "Recepcion");
                }

                // Parsear respuesta
                var respuesta = _parser.ParsearRespuestaRecepcion(responseXml);

                stopwatch.Stop();

                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║       RESPUESTA RECEPCIÓN RECIBIDA               ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  → Estado: {Estado}", respuesta.Estado);
                _logger.LogInformation("  → Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

                if (respuesta.FueRecibido)
                {
                    _logger.LogInformation("  ✓ Comprobante RECIBIDO exitosamente");
                }
                else if (respuesta.FueDevuelto)
                {
                    _logger.LogWarning("  ✗ Comprobante DEVUELTO por el SRI");
                    var errores = respuesta.ObtenerErrores();
                    foreach (var error in errores)
                    {
                        _logger.LogWarning("     Error [{Codigo}]: {Mensaje}",
                            error.Identificador, error.Mensaje);
                        if (!string.IsNullOrEmpty(error.InformacionAdicional))
                        {
                            _logger.LogWarning("     Info adicional: {Info}", error.InformacionAdicional);
                        }
                    }
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError("\n╔══════════════════════════════════════════════════╗");
                _logger.LogError("║       ERROR AL ENVIAR COMPROBANTE                ║");
                _logger.LogError("╚══════════════════════════════════════════════════╝");
                _logger.LogError("  → Tipo: {Tipo}", ex.GetType().Name);
                _logger.LogError("  → Mensaje: {Mensaje}", ex.Message);
                _logger.LogError("  → Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

                if (ex.InnerException != null)
                {
                    _logger.LogError("  → Inner Exception: {Inner}", ex.InnerException.Message);
                }

                throw;
            }
        }

        // ============================================================
        // ✨ MÉTODOS AUXILIARES PARA DECODIFICACIÓN Y DIAGNÓSTICO
        // ============================================================

        /// <summary>
        /// ✨ Decodifica el XML real del comprobante desde el SOAP envelope
        /// </summary>
        private string DecodificarXmlDesdeSoap(string soapXml)
        {
            try
            {
                _logger.LogDebug("  [DEBUG] Parseando SOAP XML...");
                var doc = XDocument.Parse(soapXml);

                _logger.LogDebug("  [DEBUG] Buscando elemento 'comprobante' o 'xml'...");

                // Buscar el elemento que contiene el XML en base64
                // Puede estar en <comprobante> o en <xml>
                var comprobante = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "comprobante" || e.Name.LocalName == "xml");

                if (comprobante == null)
                {
                    _logger.LogWarning("  [WARN] No se encontró elemento 'comprobante' ni 'xml' en el SOAP");
                    _logger.LogDebug("  [DEBUG] Elementos disponibles: {Elementos}",
                        string.Join(", ", doc.Descendants().Select(e => e.Name.LocalName).Distinct()));
                    return string.Empty;
                }

                _logger.LogDebug("  [DEBUG] Elemento encontrado: {Elemento}", comprobante.Name.LocalName);

                var base64Content = comprobante.Value?.Trim();
                if (string.IsNullOrWhiteSpace(base64Content))
                {
                    _logger.LogWarning("  [WARN] Elemento '{Elemento}' está vacío", comprobante.Name.LocalName);
                    return string.Empty;
                }

                _logger.LogDebug("  [DEBUG] Contenido Base64: {Length} caracteres", base64Content.Length);
                _logger.LogDebug("  [DEBUG] Primeros 50 caracteres: {Preview}...",
                    base64Content.Substring(0, Math.Min(50, base64Content.Length)));

                // Decodificar de base64
                _logger.LogDebug("  [DEBUG] Decodificando de Base64...");
                var xmlBytes = Convert.FromBase64String(base64Content);
                _logger.LogDebug("  [DEBUG] XML decodificado: {Size} bytes", xmlBytes.Length);

                var xmlDecodificado = Encoding.UTF8.GetString(xmlBytes);
                _logger.LogDebug("  [DEBUG] Convirtiendo a string UTF-8: {Length} caracteres", xmlDecodificado.Length);

                // Formatear el XML para mejor lectura
                _logger.LogDebug("  [DEBUG] Formateando XML...");
                var xmlDoc = XDocument.Parse(xmlDecodificado);
                var resultado = xmlDoc.ToString();

                _logger.LogInformation("  ✅ XML decodificado exitosamente: {Size} caracteres", resultado.Length);
                return resultado;
            }
            catch (FormatException fex)
            {
                _logger.LogError("  ❌ Error de formato Base64: {Error}", fex.Message);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError("  ❌ Error al decodificar XML: {Tipo} - {Error}", ex.GetType().Name, ex.Message);
                _logger.LogDebug("  [DEBUG] Stack Trace: {StackTrace}", ex.StackTrace);
                return string.Empty;
            }
        }

        /// <summary>
        /// ✨ Muestra un resumen del XML decodificado
        /// </summary>
        private void MostrarResumenXml(string xml)
        {
            try
            {
                _logger.LogDebug("  [DEBUG] Parseando XML para resumen...");
                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null)
                {
                    _logger.LogWarning("  [WARN] XML no tiene elemento raíz");
                    return;
                }

                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║   📊 RESUMEN DEL COMPROBANTE                     ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");

                // Tipo de comprobante
                _logger.LogInformation("  → Tipo: {Tipo}", root.Name.LocalName);

                // Buscar información clave
                var ns = root.GetDefaultNamespace();
                var infoTributaria = root.Element(ns + "infoTributaria");

                if (infoTributaria != null)
                {
                    _logger.LogInformation("\n  📋 INFORMACIÓN TRIBUTARIA:");

                    var ambiente = infoTributaria.Element(ns + "ambiente")?.Value;
                    var tipoEmision = infoTributaria.Element(ns + "tipoEmision")?.Value;
                    var ruc = infoTributaria.Element(ns + "ruc")?.Value;
                    var razonSocial = infoTributaria.Element(ns + "razonSocial")?.Value;
                    var claveAcceso = infoTributaria.Element(ns + "claveAcceso")?.Value;
                    var estab = infoTributaria.Element(ns + "estab")?.Value;
                    var ptoEmi = infoTributaria.Element(ns + "ptoEmi")?.Value;
                    var secuencial = infoTributaria.Element(ns + "secuencial")?.Value;
                    var codDoc = infoTributaria.Element(ns + "codDoc")?.Value;

                    if (!string.IsNullOrEmpty(ambiente))
                        _logger.LogInformation("  → Ambiente: {Ambiente}", ambiente);
                    if (!string.IsNullOrEmpty(tipoEmision))
                        _logger.LogInformation("  → Tipo Emisión: {TipoEmision}", tipoEmision);
                    if (!string.IsNullOrEmpty(ruc))
                        _logger.LogInformation("  → RUC: {Ruc}", ruc);
                    if (!string.IsNullOrEmpty(razonSocial))
                        _logger.LogInformation("  → Razón Social: {RazonSocial}", razonSocial);
                    if (!string.IsNullOrEmpty(codDoc))
                        _logger.LogInformation("  → Código Documento: {CodDoc}", codDoc);
                    if (!string.IsNullOrEmpty(estab) && !string.IsNullOrEmpty(ptoEmi) && !string.IsNullOrEmpty(secuencial))
                        _logger.LogInformation("  → Secuencial: {Estab}-{PtoEmi}-{Sec}", estab, ptoEmi, secuencial);
                    if (!string.IsNullOrEmpty(claveAcceso))
                    {
                        _logger.LogInformation("  → Clave Acceso XML: {Clave}", claveAcceso);

                        // ✨ ANALIZAR LA CLAVE DE ACCESO
                        if (claveAcceso.Length == 49)
                        {
                            _logger.LogInformation("\n  🔍 DESCOMPONIENDO CLAVE DE ACCESO:");
                            _logger.LogInformation("     [Fecha]        : {Parte} (pos 0-7)", claveAcceso.Substring(0, 8));
                            _logger.LogInformation("     [TipoDoc]      : {Parte} (pos 8-9)", claveAcceso.Substring(8, 2));
                            _logger.LogInformation("     [RUC]          : {Parte} (pos 10-22)", claveAcceso.Substring(10, 13));
                            _logger.LogInformation("     [Ambiente]     : {Parte} (pos 23)", claveAcceso.Substring(23, 1));
                            _logger.LogInformation("     [TipoEmisión]  : {Parte} (pos 24)", claveAcceso.Substring(24, 1));
                            _logger.LogInformation("     [Establecim]   : {Parte} (pos 25-27)", claveAcceso.Substring(25, 3));
                            _logger.LogInformation("     [PtoEmisión]   : {Parte} (pos 28-30)", claveAcceso.Substring(28, 3));
                            _logger.LogInformation("     [Secuencial]   : {Parte} (pos 31-39)", claveAcceso.Substring(31, 9));
                            _logger.LogInformation("     [CódNumérico]  : {Parte} (pos 40-47)", claveAcceso.Substring(40, 8));
                            _logger.LogInformation("     [DígVerif]     : {Parte} (pos 48)", claveAcceso.Substring(48, 1));
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("  ⚠️ No se encontró <infoTributaria>");
                }

                // Información de la factura (si es factura)
                var infoFactura = root.Element(ns + "infoFactura");
                if (infoFactura != null)
                {
                    _logger.LogInformation("\n  📄 INFORMACIÓN DE LA FACTURA:");

                    var fecha = infoFactura.Element(ns + "fechaEmision")?.Value;
                    var total = infoFactura.Element(ns + "importeTotal")?.Value;
                    var identificacion = infoFactura.Element(ns + "identificacionComprador")?.Value;

                    if (!string.IsNullOrEmpty(fecha))
                        _logger.LogInformation("  → Fecha Emisión: {Fecha}", fecha);
                    if (!string.IsNullOrEmpty(identificacion))
                        _logger.LogInformation("  → Identificación Comprador: {Id}", identificacion);
                    if (!string.IsNullOrEmpty(total))
                        _logger.LogInformation("  → Total: ${Total}", total);
                }

                _logger.LogInformation("\n  → Tamaño XML: {Size} caracteres", xml.Length);
                _logger.LogInformation("╚══════════════════════════════════════════════════╝\n");
            }
            catch (Exception ex)
            {
                _logger.LogError("  ❌ Error al mostrar resumen: {Error}", ex.Message);
                _logger.LogDebug("  [DEBUG] Stack Trace: {StackTrace}", ex.StackTrace);
            }
        }

        // ============================================================
        // T-076, T-077: CONSULTAR AUTORIZACIÓN
        // ============================================================

        public async Task<RespuestaAutorizacionComprobante> ConsultarAutorizacionAsync(
            AutorizacionComprobanteRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║   CONSULTANDO AUTORIZACIÓN EN EL SRI            ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  → Clave Acceso: {ClaveAcceso}", request.ClaveAcceso);
                _logger.LogInformation("  → Intento: {Intento}", request.NumeroIntento);
                _logger.LogInformation("  → URL: {Url}", _options.UrlAutorizacion);

                // Validar request
                request.Validar();

                // T-076: Construir SOAP request
                var soapRequest = request.GenerarSoapXml();

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("\n[SOAP REQUEST - AUTORIZACIÓN]");
                    _logger.LogDebug("  {Soap}", soapRequest);
                }

                // T-077: Enviar HTTP POST
                _logger.LogInformation("\n[HTTP] Consultando autorización...");
                var responseXml = await EnviarSoapRequestAsync(
                    _options.UrlAutorizacion,
                    soapRequest,
                    "Autorizacion",
                    cancellationToken);

                if (_options.LogSoapDetallado)
                {
                    _parser.LogRespuestaSoap(responseXml, "Autorizacion");
                }

                // Parsear respuesta
                var respuesta = _parser.ParsearRespuestaAutorizacion(responseXml);

                stopwatch.Stop();

                var autorizacion = respuesta.PrimeraAutorizacion;
                var estado = autorizacion?.Estado ?? "SIN AUTORIZACION";

                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║     RESPUESTA AUTORIZACIÓN RECIBIDA              ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  → Estado: {Estado}", estado);
                _logger.LogInformation("  → Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

                if (autorizacion != null)
                {
                    if (autorizacion.EstaAutorizado)
                    {
                        _logger.LogInformation("  🎉 Comprobante AUTORIZADO");
                        _logger.LogInformation("  → Número Autorización: {Numero}", autorizacion.NumeroAutorizacion);
                        _logger.LogInformation("  → Fecha: {Fecha}", autorizacion.FechaAutorizacion);
                        _logger.LogInformation("  → Ambiente: {Ambiente}", autorizacion.Ambiente);
                    }
                    else if (autorizacion.EnProcesamiento)
                    {
                        _logger.LogInformation("  ⏳ Comprobante EN PROCESAMIENTO");
                        _logger.LogInformation("  → El SRI aún está procesando el comprobante");
                    }
                    else if (autorizacion.EstaNoAutorizado)
                    {
                        _logger.LogWarning("  ❌ Comprobante NO AUTORIZADO");
                        foreach (var mensaje in autorizacion.Mensajes)
                        {
                            _logger.LogWarning("  → Mensaje SRI: {Mensaje}", mensaje);
                        }
                    }

                    // Log de mensajes adicionales
                    if (autorizacion.Mensajes.Any())
                    {
                        _logger.LogInformation("\n  Mensajes del SRI:");
                        foreach (var mensaje in autorizacion.Mensajes)
                        {
                            _logger.LogInformation("  • {Mensaje}", mensaje);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("  ⚠️ No se recibió información de autorización");
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError("\n╔══════════════════════════════════════════════════╗");
                _logger.LogError("║     ERROR AL CONSULTAR AUTORIZACIÓN              ║");
                _logger.LogError("╚══════════════════════════════════════════════════╝");
                _logger.LogError("  → Tipo: {Tipo}", ex.GetType().Name);
                _logger.LogError("  → Mensaje: {Mensaje}", ex.Message);
                _logger.LogError("  → Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

                if (ex.InnerException != null)
                {
                    _logger.LogError("  → Inner Exception: {Inner}", ex.InnerException.Message);
                }

                throw;
            }
        }

        // ============================================================
        // T-077: ENVÍO HTTP GENÉRICO CON LOGGING DETALLADO
        // ============================================================

        private async Task<string> EnviarSoapRequestAsync(
            string url,
            string soapXml,
            string operacion,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("\n[HTTP-{Operacion}] Preparando envío...", operacion);
                _logger.LogDebug("  → URL: {Url}", url);
                _logger.LogDebug("  → Tamaño SOAP: {Size} bytes", soapXml.Length);

                // Crear contenido HTTP
                var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
                content.Headers.ContentType!.CharSet = "utf-8";

                if (!content.Headers.Contains("SOAPAction"))
                {
                    content.Headers.Add("SOAPAction", "");
                }

                _logger.LogDebug("  → Headers configurados:");
                _logger.LogDebug("     Content-Type: text/xml; charset=utf-8");
                _logger.LogDebug("     SOAPAction: \"\"");

                // Enviar POST
                _logger.LogDebug("  → Enviando request... (timeout: {Timeout}s)", _httpClient.Timeout.TotalSeconds);

                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                stopwatch.Stop();
                _logger.LogDebug("  ✓ Respuesta recibida en {Ms}ms", stopwatch.ElapsedMilliseconds);

                // Leer respuesta
                var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

                // Log detallado del código HTTP
                _logger.LogInformation("  → HTTP Status: {StatusCode} {StatusText}",
                    (int)response.StatusCode, response.StatusCode);
                _logger.LogInformation("  → Tamaño respuesta: {Size} bytes", responseXml.Length);

                // Verificar errores HTTP
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("\n  ❌ ERROR HTTP {StatusCode}:", response.StatusCode);
                    _logger.LogError("  Respuesta completa (primeros 1000 caracteres):");
                    _logger.LogError("  {Response}",
                        responseXml.Substring(0, Math.Min(1000, responseXml.Length)));

                    // Intentar extraer mensaje de error SOAP Fault
                    var errorMsg = _parser.ExtraerMensajeErrorSoapFault(responseXml);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        throw new HttpRequestException(
                            $"Error SOAP del SRI: {errorMsg} (HTTP {response.StatusCode})");
                    }

                    response.EnsureSuccessStatusCode();
                }

                // Validar que sea respuesta SOAP válida
                if (!_parser.EsRespuestaSoapValida(responseXml))
                {
                    _logger.LogError("  ✗ La respuesta NO es un XML SOAP válido");
                    _logger.LogError("  Primeros 500 caracteres:");
                    _logger.LogError("  {Response}",
                        responseXml.Substring(0, Math.Min(500, responseXml.Length)));

                    throw new InvalidOperationException(
                        "La respuesta del SRI no es un XML SOAP válido");
                }

                _logger.LogDebug("  ✓ Respuesta SOAP válida");

                return responseXml;
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError("\n  ✗ TIMEOUT después de {Timeout}s", _httpClient.Timeout.TotalSeconds);
                _logger.LogError("  → Operación: {Operacion}", operacion);
                _logger.LogError("  → Tiempo transcurrido: {Ms}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("  → Mensaje: {Message}", ex.Message);

                throw new TimeoutException(
                    $"Timeout al comunicarse con el SRI después de {_options.TimeoutSegundos}s", ex);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError("\n  ✗ ERROR HTTP al comunicarse con el SRI");
                _logger.LogError("  → Operación: {Operacion}", operacion);
                _logger.LogError("  → Tiempo: {Ms}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("  → StatusCode: {StatusCode}", ex.StatusCode);
                _logger.LogError("  → Mensaje: {Message}", ex.Message);

                // Log de excepciones internas
                var innerEx = ex.InnerException;
                int nivel = 1;
                while (innerEx != null)
                {
                    _logger.LogError("  → Inner Exception (nivel {Nivel}): {Type}",
                        nivel, innerEx.GetType().Name);
                    _logger.LogError("     Mensaje: {Message}", innerEx.Message);
                    innerEx = innerEx.InnerException;
                    nivel++;
                }

                throw new InvalidOperationException(
                    "Error de red al comunicarse con el SRI. Verifique conectividad.", ex);
            }
        }

        // ============================================================
        // VERIFICACIÓN DE CONECTIVIDAD CON LOGGING MEJORADO
        // ============================================================

        public async Task<bool> VerificarConectividadAsync()
        {
            try
            {
                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║     VERIFICANDO CONECTIVIDAD CON EL SRI          ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  → URL Recepción: {Url}", _options.UrlRecepcion);

                var request = new HttpRequestMessage(HttpMethod.Head, _options.UrlRecepcion);
                var response = await _httpClient.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead);

                // 405 Method Not Allowed es aceptable (el servidor existe)
                var conectado = response.IsSuccessStatusCode ||
                                response.StatusCode == HttpStatusCode.MethodNotAllowed;

                if (conectado)
                {
                    _logger.LogInformation("  ✅ Conectividad OK");
                    _logger.LogInformation("  → HTTP Status: {Status}", response.StatusCode);
                }
                else
                {
                    _logger.LogWarning("  ❌ No disponible");
                    _logger.LogWarning("  → HTTP Status: {Status}", response.StatusCode);
                }

                return conectado;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("\n  ⚠️ No se pudo verificar conectividad");
                _logger.LogWarning("  → Tipo: {Type}", ex.GetType().Name);
                _logger.LogWarning("  → Mensaje: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<EstadoServiciosSri> ObtenerEstadoServiciosAsync()
        {
            var estado = new EstadoServiciosSri();

            _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
            _logger.LogInformation("║     VERIFICANDO ESTADO DE SERVICIOS SRI          ║");
            _logger.LogInformation("╚══════════════════════════════════════════════════╝");

            try
            {
                // Verificar servicio de recepción
                var swRecepcion = Stopwatch.StartNew();
                _logger.LogInformation("\n[1/2] Verificando Servicio de Recepción...");

                try
                {
                    var requestRecepcion = new HttpRequestMessage(
                        HttpMethod.Head, _options.UrlRecepcion);
                    var responseRecepcion = await _httpClient.SendAsync(
                        requestRecepcion, HttpCompletionOption.ResponseHeadersRead);

                    estado.RecepcionDisponible = responseRecepcion.IsSuccessStatusCode ||
                        responseRecepcion.StatusCode == HttpStatusCode.MethodNotAllowed;
                    estado.TiempoRespuestaRecepcionMs = swRecepcion.ElapsedMilliseconds;

                    _logger.LogInformation("  → Estado: {Estado}",
                        estado.RecepcionDisponible ? "✅ Disponible" : "❌ No disponible");
                    _logger.LogInformation("  → Tiempo: {Ms}ms", estado.TiempoRespuestaRecepcionMs);
                    _logger.LogInformation("  → HTTP Status: {Status}", responseRecepcion.StatusCode);
                }
                catch (Exception ex)
                {
                    estado.RecepcionDisponible = false;
                    estado.UltimoError = ex.Message;
                    _logger.LogWarning("  → Error: {Message}", ex.Message);
                }
                swRecepcion.Stop();

                // Verificar servicio de autorización
                var swAutorizacion = Stopwatch.StartNew();
                _logger.LogInformation("\n[2/2] Verificando Servicio de Autorización...");

                try
                {
                    var requestAutorizacion = new HttpRequestMessage(
                        HttpMethod.Head, _options.UrlAutorizacion);
                    var responseAutorizacion = await _httpClient.SendAsync(
                        requestAutorizacion, HttpCompletionOption.ResponseHeadersRead);

                    estado.AutorizacionDisponible = responseAutorizacion.IsSuccessStatusCode ||
                        responseAutorizacion.StatusCode == HttpStatusCode.MethodNotAllowed;
                    estado.TiempoRespuestaAutorizacionMs = swAutorizacion.ElapsedMilliseconds;

                    _logger.LogInformation("  → Estado: {Estado}",
                        estado.AutorizacionDisponible ? "✅ Disponible" : "❌ No disponible");
                    _logger.LogInformation("  → Tiempo: {Ms}ms", estado.TiempoRespuestaAutorizacionMs);
                    _logger.LogInformation("  → HTTP Status: {Status}", responseAutorizacion.StatusCode);
                }
                catch (Exception ex)
                {
                    estado.AutorizacionDisponible = false;
                    if (string.IsNullOrEmpty(estado.UltimoError))
                    {
                        estado.UltimoError = ex.Message;
                    }
                    _logger.LogWarning("  → Error: {Message}", ex.Message);
                }
                swAutorizacion.Stop();

                _logger.LogInformation("\n╔══════════════════════════════════════════════════╗");
                _logger.LogInformation("║     RESUMEN ESTADO SERVICIOS                     ║");
                _logger.LogInformation("╚══════════════════════════════════════════════════╝");
                _logger.LogInformation("  Recepción:     {Estado} ({Ms}ms)",
                    estado.RecepcionDisponible ? "✅" : "❌",
                    estado.TiempoRespuestaRecepcionMs);
                _logger.LogInformation("  Autorización:  {Estado} ({Ms}ms)",
                    estado.AutorizacionDisponible ? "✅" : "❌",
                    estado.TiempoRespuestaAutorizacionMs);

                return estado;
            }
            catch (Exception ex)
            {
                _logger.LogError("\n  ❌ Error al obtener estado de servicios");
                _logger.LogError("  → Mensaje: {Message}", ex.Message);
                estado.UltimoError = ex.Message;
                return estado;
            }
        }
    }
}