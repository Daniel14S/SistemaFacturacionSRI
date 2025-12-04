// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriWebServiceClient.cs
// T-076: Construcción de requests SOAP
// T-077: Envío HTTP al SRI

using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-076, T-077: Cliente para consumir WebServices SOAP del SRI
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

            // Validar configuración
            _options.Validar();

            // Configurar HttpClient
            ConfigurarHttpClient();
        }

        // ============================================================
        // T-077: CONFIGURACIÓN HTTP CLIENT
        // ============================================================

        /// <summary>
        /// T-077: Configura el HttpClient con las opciones necesarias
        /// </summary>
        private void ConfigurarHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSegundos);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/xml, application/xml");

            _logger.LogDebug("HttpClient configurado: Timeout={Timeout}s, UserAgent={UserAgent}",
                _options.TimeoutSegundos, _options.UserAgent);
        }

        // ============================================================
        // T-076, T-077: ENVIAR COMPROBANTE (RECEPCIÓN)
        // ============================================================

        /// <summary>
        /// T-076, T-077: Envía un comprobante al SRI para recepción
        /// </summary>
        public async Task<RespuestaRecepcionComprobante> EnviarComprobanteAsync(
            RecepcionComprobanteRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("ENVIANDO COMPROBANTE AL SRI");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("Clave Acceso: {ClaveAcceso}", request.ClaveAcceso);
                _logger.LogInformation("URL: {Url}", _options.UrlRecepcion);

                // Validar request
                request.Validar();

                // T-076: Construir SOAP request
                var soapRequest = request.GenerarSoapXml();

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("Request SOAP:\n{Soap}", soapRequest);
                }

                // T-077: Enviar HTTP POST
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

                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("✅ RESPUESTA RECEPCIÓN RECIBIDA");
                _logger.LogInformation("Estado: {Estado}", respuesta.Estado);
                _logger.LogInformation("Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("═══════════════════════════════════════");

                if (respuesta.FueDevuelto)
                {
                    var errores = respuesta.ObtenerErrores();
                    foreach (var error in errores)
                    {
                        _logger.LogWarning("Error SRI: [{Codigo}] {Mensaje}", 
                            error.Identificador, error.Mensaje);
                    }
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error al enviar comprobante. Tiempo: {Tiempo}ms", 
                    stopwatch.ElapsedMilliseconds);
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

            try
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("CONSULTANDO AUTORIZACIÓN EN EL SRI");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("Clave Acceso: {ClaveAcceso}", request.ClaveAcceso);
                _logger.LogInformation("Intento: {Intento}", request.NumeroIntento);
                _logger.LogInformation("URL: {Url}", _options.UrlAutorizacion);

                // Validar request
                request.Validar();

                // T-076: Construir SOAP request
                var soapRequest = request.GenerarSoapXml();

                if (_options.LogSoapDetallado)
                {
                    _logger.LogDebug("Request SOAP:\n{Soap}", soapRequest);
                }

                // T-077: Enviar HTTP POST
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

                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("✅ RESPUESTA AUTORIZACIÓN RECIBIDA");
                _logger.LogInformation("Estado: {Estado}", estado);
                _logger.LogInformation("Tiempo: {Tiempo}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("═══════════════════════════════════════");

                if (autorizacion != null)
                {
                    if (autorizacion.EstaAutorizado)
                    {
                        _logger.LogInformation("🎉 Comprobante AUTORIZADO");
                        _logger.LogInformation("Número Autorización: {Numero}", 
                            autorizacion.NumeroAutorizacion);
                        _logger.LogInformation("Fecha: {Fecha}", autorizacion.FechaAutorizacion);
                    }
                    else if (autorizacion.EnProcesamiento)
                    {
                        _logger.LogInformation("⏳ Comprobante EN PROCESAMIENTO");
                    }
                    else if (autorizacion.EstaNoAutorizado)
                    {
                        _logger.LogWarning("❌ Comprobante NO AUTORIZADO");
                        foreach (var mensaje in autorizacion.Mensajes)
                        {
                            _logger.LogWarning("Mensaje SRI: {Mensaje}", mensaje);
                        }
                    }
                }

                return respuesta;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error al consultar autorización. Tiempo: {Tiempo}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        // ============================================================
        // T-077: ENVÍO HTTP GENÉRICO
        // ============================================================

        /// <summary>
        /// T-077: Envía un request SOAP genérico por HTTP POST
        /// </summary>
        private async Task<string> EnviarSoapRequestAsync(
            string url,
            string soapXml,
            string operacion,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Enviando request SOAP a: {Url}", url);

                // Crear contenido HTTP
                var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
                
                // Headers SOAP requeridos
                content.Headers.ContentType!.CharSet = "utf-8";
                
                // SOAPAction vacío según especificación SRI
                if (!content.Headers.Contains("SOAPAction"))
                {
                    content.Headers.Add("SOAPAction", "");
                }

                // Enviar POST
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                // Leer respuesta
                var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

                // Log de código HTTP
                _logger.LogDebug("Respuesta HTTP: {StatusCode}", response.StatusCode);

                // Verificar errores HTTP
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error HTTP {StatusCode}: {Response}", 
                        response.StatusCode, responseXml);
                    
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
                    throw new InvalidOperationException(
                        "La respuesta del SRI no es un XML SOAP válido");
                }

                return responseXml;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout al comunicarse con el SRI");
                throw new TimeoutException(
                    $"Timeout al comunicarse con el SRI después de {_options.TimeoutSegundos}s", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de red al comunicarse con el SRI");
                throw new InvalidOperationException(
                    "Error de red al comunicarse con el SRI. Verifique conectividad.", ex);
            }
        }

        // ============================================================
        // VERIFICACIÓN DE CONECTIVIDAD
        // ============================================================

        /// <summary>
        /// Verifica la conectividad con el servidor del SRI
        /// </summary>
        public async Task<bool> VerificarConectividadAsync()
        {
            try
            {
                _logger.LogDebug("Verificando conectividad con el SRI...");

                // Intentar hacer HEAD request a la URL de recepción
                var request = new HttpRequestMessage(HttpMethod.Head, _options.UrlRecepcion);
                var response = await _httpClient.SendAsync(request, 
                    HttpCompletionOption.ResponseHeadersRead);

                // 405 Method Not Allowed es aceptable (el servidor existe)
                var conectado = response.IsSuccessStatusCode || 
                                response.StatusCode == HttpStatusCode.MethodNotAllowed;

                _logger.LogDebug("Conectividad con SRI: {Estado}", 
                    conectado ? "✅ OK" : "❌ No disponible");

                return conectado;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo verificar conectividad con el SRI");
                return false;
            }
        }

        /// <summary>
        /// Obtiene el estado actual de los servicios del SRI
        /// </summary>
        public async Task<EstadoServiciosSri> ObtenerEstadoServiciosAsync()
        {
            var estado = new EstadoServiciosSri();

            try
            {
                // Verificar servicio de recepción
                var swRecepcion = Stopwatch.StartNew();
                try
                {
                    var requestRecepcion = new HttpRequestMessage(
                        HttpMethod.Head, _options.UrlRecepcion);
                    var responseRecepcion = await _httpClient.SendAsync(
                        requestRecepcion, HttpCompletionOption.ResponseHeadersRead);
                    
                    estado.RecepcionDisponible = responseRecepcion.IsSuccessStatusCode ||
                        responseRecepcion.StatusCode == HttpStatusCode.MethodNotAllowed;
                    estado.TiempoRespuestaRecepcionMs = swRecepcion.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    estado.RecepcionDisponible = false;
                    estado.UltimoError = ex.Message;
                }
                swRecepcion.Stop();

                // Verificar servicio de autorización
                var swAutorizacion = Stopwatch.StartNew();
                try
                {
                    var requestAutorizacion = new HttpRequestMessage(
                        HttpMethod.Head, _options.UrlAutorizacion);
                    var responseAutorizacion = await _httpClient.SendAsync(
                        requestAutorizacion, HttpCompletionOption.ResponseHeadersRead);
                    
                    estado.AutorizacionDisponible = responseAutorizacion.IsSuccessStatusCode ||
                        responseAutorizacion.StatusCode == HttpStatusCode.MethodNotAllowed;
                    estado.TiempoRespuestaAutorizacionMs = swAutorizacion.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    estado.AutorizacionDisponible = false;
                    if (string.IsNullOrEmpty(estado.UltimoError))
                    {
                        estado.UltimoError = ex.Message;
                    }
                }
                swAutorizacion.Stop();

                _logger.LogInformation("Estado servicios SRI: Recepción={Recepcion}, Autorización={Autorizacion}",
                    estado.RecepcionDisponible ? "✅" : "❌",
                    estado.AutorizacionDisponible ? "✅" : "❌");

                return estado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de servicios SRI");
                estado.UltimoError = ex.Message;
                return estado;
            }
        }
    }
}