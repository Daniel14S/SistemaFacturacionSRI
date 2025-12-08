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

        // 🔍 AGREGAR: Directorio para logs de debugging
        private readonly string _debugPath;

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

        public async Task<RespuestaRecepcionComprobante> EnviarComprobanteAsync(
            RecepcionComprobanteRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var claveAcceso = request.ClaveAcceso;

            try
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("ENVIANDO COMPROBANTE AL SRI");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("Clave Acceso: {ClaveAcceso}", request.ClaveAcceso);
                _logger.LogInformation("URL: {Url}", _options.UrlRecepcion);

                request.Validar();

                // Construir SOAP request
                var soapRequest = request.GenerarSoapXml();

                // 🔍 GUARDAR REQUEST
                var requestFile = Path.Combine(_debugPath, $"{timestamp}_{claveAcceso}_REQUEST.xml");
                await File.WriteAllTextAsync(requestFile, soapRequest, cancellationToken);
                _logger.LogInformation("📄 Request guardado en: {File}", requestFile);

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("\n[SOAP REQUEST COMPLETO]");
                    _logger.LogDebug("{Soap}", soapRequest);
                }

                // Enviar HTTP POST
                var responseXml = await EnviarSoapRequestAsync(
                    _options.UrlRecepcion,
                    soapRequest,
                    "Recepcion",
                    cancellationToken);

                // 🔍 GUARDAR RESPONSE
                var responseFile = Path.Combine(_debugPath, $"{timestamp}_{claveAcceso}_RESPONSE.xml");
                await File.WriteAllTextAsync(responseFile, responseXml, cancellationToken);
                _logger.LogInformation("📄 Response guardado en: {File}", responseFile);

                if (_options.LogSoapDetallado)
                {
                    _parser.LogRespuestaSoap(responseXml, "Recepcion");
                }

                // 🔍 LOG COMPLETO DEL XML DE RESPUESTA
                _logger.LogWarning("═══════════════════════════════════════");
                _logger.LogWarning("📋 RESPONSE XML COMPLETO:");
                _logger.LogWarning("{ResponseXml}", responseXml);
                _logger.LogWarning("═══════════════════════════════════════");

                // Parsear respuesta
                var respuesta = _parser.ParsearRespuestaRecepcion(responseXml);

                stopwatch.Stop();

                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("✅ RESPUESTA RECEPCIÓN RECIBIDA");
                _logger.LogInformation("Estado: {Estado}", respuesta.Estado);
                
                // 🔍 LOG DETALLADO DE LA RESPUESTA PARSEADA
                _logger.LogWarning("🔍 DETALLES DE RESPUESTA PARSEADA:");
                _logger.LogWarning("   - Estado: {Estado}", respuesta.Estado);
                _logger.LogWarning("   - FueRecibido: {Recibido}", respuesta.FueRecibido);
                _logger.LogWarning("   - FueDevuelto: {Devuelto}", respuesta.FueDevuelto);
                _logger.LogWarning("   - Cantidad Comprobantes: {Cantidad}", respuesta.Comprobantes?.Count ?? 0);
                
                if (respuesta.Comprobantes?.Any() == true)
                {
                    var comp = respuesta.Comprobantes.First();
                    _logger.LogWarning("   - Comprobante ClaveAcceso: {Clave}", comp.ClaveAcceso);
                    _logger.LogWarning("   - Comprobante Estado: {Estado}", comp.Estado);
                    _logger.LogWarning("   - Mensajes Count: {Count}", comp.Mensajes?.Count ?? 0);
                    
                    if (comp.Mensajes?.Any() == true)
                    {
                        foreach (var msg in comp.Mensajes)
                        {
                            _logger.LogWarning("      * [{Id}] {Tipo}: {Mensaje}", 
                                msg.Identificador, msg.Tipo, msg.Mensaje);
                            _logger.LogWarning("        Info Adicional: {Info}", msg.InformacionAdicional);
                        }
                    }
                }
                
                _logger.LogInformation("Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("═══════════════════════════════════════");

                if (respuesta.FueDevuelto)
                {
                    _logger.LogWarning("  ✗ Comprobante DEVUELTO por el SRI");
                    var errores = respuesta.ObtenerErrores();
                    _logger.LogError("❌ COMPROBANTE DEVUELTO - {Count} errores", errores.Count);
                    foreach (var error in errores)
                    {
                        _logger.LogError("   [{Codigo}] {Tipo}: {Mensaje}", 
                            error.Identificador, error.Tipo, error.Mensaje);
                        if (!string.IsNullOrEmpty(error.InformacionAdicional))
                        {
                            _logger.LogError("   Info adicional: {Info}", error.InformacionAdicional);
                        }
                    }
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // 🔍 GUARDAR EXCEPCIÓN
                var errorFile = Path.Combine(_debugPath, $"{timestamp}_{claveAcceso}_ERROR.txt");
                var errorContent = $"Timestamp: {timestamp}\n" +
                                 $"ClaveAcceso: {claveAcceso}\n" +
                                 $"Exception: {ex.GetType().Name}\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"StackTrace:\n{ex.StackTrace}\n" +
                                 $"\nInnerException: {ex.InnerException?.Message}";
                await File.WriteAllTextAsync(errorFile, errorContent);
                
                _logger.LogError("\n╔══════════════════════════════════════════════════╗");
                _logger.LogError("║       ERROR AL ENVIAR COMPROBANTE                ║");
                _logger.LogError("╚══════════════════════════════════════════════════╝");
                _logger.LogError("  → Tipo: {Tipo}", ex.GetType().Name);
                _logger.LogError("  → Mensaje: {Mensaje}", ex.Message);
                _logger.LogError("  → Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("Error guardado en: {File}", errorFile);

                if (ex.InnerException != null)
                {
                    _logger.LogError("  → Inner Exception: {Inner}", ex.InnerException.Message);
                }

                throw;
            }
        }

        // ============================================================
        // T-076, T-077: CONSULTAR AUTORIZACIÓN
        // ============================================================

        /// <summary>
        /// T-076, T-077: Consulta la autorización de un comprobante
        /// </summary>
        public async Task<RespuestaAutorizacionComprobante> ConsultarAutorizacionAsync(
            AutorizacionComprobanteRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var claveAcceso = request.ClaveAcceso;

            try
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("CONSULTANDO AUTORIZACIÓN EN EL SRI");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("Clave Acceso: {ClaveAcceso}", claveAcceso);
                _logger.LogInformation("Intento: {Intento}", request.NumeroIntento);
                _logger.LogInformation("URL: {Url}", _options.UrlAutorizacion);

                request.Validar();

                var soapRequest = request.GenerarSoapXml();

                // 🔍 GUARDAR REQUEST DE AUTORIZACIÓN
                var requestFile = Path.Combine(_debugPath, $"{timestamp}_{claveAcceso}_AUTH_REQUEST.xml");
                await File.WriteAllTextAsync(requestFile, soapRequest, cancellationToken);

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("\n[SOAP REQUEST - AUTORIZACIÓN]");
                    _logger.LogDebug("  {Soap}", soapRequest);
                }

                var responseXml = await EnviarSoapRequestAsync(
                    _options.UrlAutorizacion,
                    soapRequest,
                    "Autorizacion",
                    cancellationToken);

                // 🔍 GUARDAR RESPONSE DE AUTORIZACIÓN
                var responseFile = Path.Combine(_debugPath, $"{timestamp}_{claveAcceso}_AUTH_RESPONSE.xml");
                await File.WriteAllTextAsync(responseFile, responseXml, cancellationToken);

                if (_options.LogSoapDetallado)
                {
                    _parser.LogRespuestaSoap(responseXml, "Autorizacion");
                }

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

                var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
                content.Headers.ContentType!.CharSet = "utf-8";
                
                if (!content.Headers.Contains("SOAPAction"))
                {
                    content.Headers.Add("SOAPAction", "");
                }

                _logger.LogDebug("  → Headers configurados:");
                _logger.LogDebug("     Content-Type: text/xml; charset=utf-8");
                _logger.LogDebug("     SOAPAction: \"\"");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug("Respuesta HTTP: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error HTTP {StatusCode}: {Response}", 
                        response.StatusCode, responseXml);
                    
                    var errorMsg = _parser.ExtraerMensajeErrorSoapFault(responseXml);
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        throw new HttpRequestException(
                            $"Error SOAP del SRI: {errorMsg} (HTTP {response.StatusCode})");
                    }

                    response.EnsureSuccessStatusCode();
                }

                if (!_parser.EsRespuestaSoapValida(responseXml))
                {
                    _logger.LogError("❌ Respuesta NO es XML SOAP válido:\n{Xml}", responseXml);
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