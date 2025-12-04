// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriComprobanteService.cs
// T-078: Implementación de envío de comprobantes
// T-079: Implementación de consulta de autorización
// T-080: Lógica de reintentos

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-078, T-079: Servicio de alto nivel para enviar y autorizar comprobantes en el SRI
    /// </summary>
    public class SriComprobanteService
    {
        private readonly ISriWebServiceClient _sriClient;
        private readonly SriWebServicesOptions _options;
        private readonly ILogger<SriComprobanteService> _logger;

        public SriComprobanteService(
            ISriWebServiceClient sriClient,
            IOptions<SriWebServicesOptions> options,
            ILogger<SriComprobanteService> logger)
        {
            _sriClient = sriClient ?? throw new ArgumentNullException(nameof(sriClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _options.Validar();
        }

        // ============================================================
        // T-078: ENVIAR COMPROBANTE (RECEPCIÓN)
        // ============================================================

        /// <summary>
        /// T-078: Envía un comprobante firmado al SRI para recepción
        /// Incluye lógica de reintentos automática
        /// </summary>
        public async Task<ResultadoOperacionSri> EnviarComprobanteAsync(
            string xmlFirmado,
            string claveAcceso,
            string rucEmisor,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var intentos = 0;
            Exception? ultimaExcepcion = null;

            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("T-078: INICIANDO ENVÍO DE COMPROBANTE AL SRI");
            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("Clave Acceso: {ClaveAcceso}", claveAcceso);
            _logger.LogInformation("RUC Emisor: {Ruc}", rucEmisor);
            _logger.LogInformation("Reintentos máximos: {Max}", _options.ReintentoMaximo);

            // T-080: Lógica de reintentos
            for (intentos = 1; intentos <= _options.ReintentoMaximo; intentos++)
            {
                try
                {
                    _logger.LogInformation("📤 Intento {Intento}/{Max} - Enviando comprobante...",
                        intentos, _options.ReintentoMaximo);

                    // Crear request
                    var request = new RecepcionComprobanteRequest
                    {
                        XmlComprobante = xmlFirmado,
                        ClaveAcceso = claveAcceso,
                        RucEmisor = rucEmisor,
                        FechaEmision = DateTime.Now
                    };

                    // Enviar al SRI
                    var respuesta = await _sriClient.EnviarComprobanteAsync(request, cancellationToken);

                    stopwatch.Stop();

                    // Procesar respuesta
                    if (respuesta.FueRecibido)
                    {
                        _logger.LogInformation("✅ Comprobante RECIBIDO por el SRI");
                        _logger.LogInformation("Estado: {Estado}", respuesta.Estado);
                        _logger.LogInformation("Tiempo total: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

                        var informativos = respuesta.ObtenerInformativos();
                        foreach (var info in informativos)
                        {
                            _logger.LogInformation("ℹ️  {Mensaje}", info.Mensaje);
                        }

                        return new ResultadoOperacionSri
                        {
                            Exitoso = true,
                            ClaveAcceso = claveAcceso,
                            Estado = EstadosComprobanteSri.RECIBIDA,
                            NumeroAutorizacion = string.Empty, // Se obtiene después
                            FechaAutorizacion = DateTime.Now,
                            XmlAutorizado = xmlFirmado,
                            Mensajes = respuesta.Comprobantes.SelectMany(c => c.Mensajes).ToList(),
                            NumeroIntentos = intentos,
                            TiempoTranscurrido = stopwatch.Elapsed
                        };
                    }
                    else if (respuesta.FueDevuelto)
                    {
                        _logger.LogWarning("❌ Comprobante DEVUELTO por el SRI");
                        _logger.LogWarning("Estado: {Estado}", respuesta.Estado);

                        var errores = respuesta.ObtenerErrores();
                        var mensajesError = errores.Select(e => $"[{e.Identificador}] {e.Mensaje}").ToList();

                        foreach (var error in errores)
                        {
                            _logger.LogError("Error SRI: [{Codigo}] {Mensaje}",
                                error.Identificador, error.Mensaje);
                        }

                        stopwatch.Stop();

                        // No reintentar en errores de validación
                        return new ResultadoOperacionSri
                        {
                            Exitoso = false,
                            ClaveAcceso = claveAcceso,
                            Estado = EstadosComprobanteSri.DEVUELTA,
                            MensajeError = string.Join("; ", mensajesError),
                            Mensajes = respuesta.Comprobantes.SelectMany(c => c.Mensajes).ToList(),
                            NumeroIntentos = intentos,
                            TiempoTranscurrido = stopwatch.Elapsed
                        };
                    }
                }
                catch (TimeoutException ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex, "⏱️  Timeout en intento {Intento}/{Max}",
                        intentos, _options.ReintentoMaximo);

                    if (intentos < _options.ReintentoMaximo)
                    {
                        var delay = _options.CalcularDelay(intentos);
                        _logger.LogInformation("⏳ Esperando {Delay}ms antes del siguiente intento...", delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (HttpRequestException ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex, "🌐 Error de red en intento {Intento}/{Max}",
                        intentos, _options.ReintentoMaximo);

                    if (intentos < _options.ReintentoMaximo)
                    {
                        var delay = _options.CalcularDelay(intentos);
                        _logger.LogInformation("⏳ Esperando {Delay}ms antes del siguiente intento...", delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogError(ex, "❌ Error inesperado en intento {Intento}/{Max}",
                        intentos, _options.ReintentoMaximo);

                    // No reintentar en errores desconocidos
                    break;
                }
            }

            // Si llegamos aquí, fallaron todos los intentos
            stopwatch.Stop();
            _logger.LogError("❌ FALLO DESPUÉS DE {Intentos} INTENTOS", intentos);
            _logger.LogError("Tiempo total: {Tiempo}ms", stopwatch.ElapsedMilliseconds);

            return new ResultadoOperacionSri
            {
                Exitoso = false,
                ClaveAcceso = claveAcceso,
                Estado = "ERROR",
                MensajeError = $"Error al enviar comprobante después de {intentos} intentos: {ultimaExcepcion?.Message}",
                NumeroIntentos = intentos,
                TiempoTranscurrido = stopwatch.Elapsed
            };
        }

        // ============================================================
        // T-079: CONSULTAR AUTORIZACIÓN
        // ============================================================

        /// <summary>
        /// T-079: Consulta la autorización de un comprobante
        /// Incluye lógica de reintentos cuando está EN PROCESAMIENTO
        /// </summary>
        public async Task<ResultadoOperacionSri> ConsultarAutorizacionAsync(
            string claveAcceso,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var intentos = 0;
            var maxIntentosConsulta = _options.MaximosIntentosConsulta;

            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("T-079: CONSULTANDO AUTORIZACIÓN EN EL SRI");
            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("Clave Acceso: {ClaveAcceso}", claveAcceso);
            _logger.LogInformation("Intentos máximos: {Max}", maxIntentosConsulta);

            Exception? ultimaExcepcion = null;

            for (intentos = 1; intentos <= maxIntentosConsulta; intentos++)
            {
                try
                {
                    _logger.LogInformation("🔍 Intento {Intento}/{Max} - Consultando autorización...",
                        intentos, maxIntentosConsulta);

                    // Crear request
                    var request = new AutorizacionComprobanteRequest
                    {
                        ClaveAcceso = claveAcceso,
                        NumeroIntento = intentos
                    };

                    // Consultar en el SRI
                    var respuesta = await _sriClient.ConsultarAutorizacionAsync(request, cancellationToken);

                    var autorizacion = respuesta.PrimeraAutorizacion;

                    if (autorizacion == null)
                    {
                        _logger.LogWarning("⚠️  No se encontró información de autorización");

                        if (intentos < maxIntentosConsulta)
                        {
                            await EsperarEntreConsultas(intentos, cancellationToken);
                            continue;
                        }

                        stopwatch.Stop();
                        return new ResultadoOperacionSri
                        {
                            Exitoso = false,
                            ClaveAcceso = claveAcceso,
                            Estado = "SIN_AUTORIZACION",
                            MensajeError = "No se encontró información de autorización",
                            NumeroIntentos = intentos,
                            TiempoTranscurrido = stopwatch.Elapsed
                        };
                    }

                    // CASO 1: AUTORIZADO ✅
                    if (autorizacion.EstaAutorizado)
                    {
                        _logger.LogInformation("🎉 Comprobante AUTORIZADO");
                        _logger.LogInformation("Número Autorización: {Numero}", autorizacion.NumeroAutorizacion);
                        _logger.LogInformation("Fecha: {Fecha}", autorizacion.FechaAutorizacion);

                        stopwatch.Stop();

                        autorizacion.ParsearFechaAutorizacion();

                        return new ResultadoOperacionSri
                        {
                            Exitoso = true,
                            ClaveAcceso = claveAcceso,
                            Estado = EstadosComprobanteSri.AUTORIZADO,
                            NumeroAutorizacion = autorizacion.NumeroAutorizacion,
                            FechaAutorizacion = autorizacion.FechaAutorizacionParsed ?? DateTime.Now,
                            XmlAutorizado = autorizacion.Comprobante,
                            Mensajes = autorizacion.Mensajes,
                            NumeroIntentos = intentos,
                            TiempoTranscurrido = stopwatch.Elapsed
                        };
                    }

                    // CASO 2: NO AUTORIZADO ❌
                    if (autorizacion.EstaNoAutorizado)
                    {
                        _logger.LogWarning("❌ Comprobante NO AUTORIZADO");

                        var mensajesError = autorizacion.Mensajes
                            .Select(m => $"[{m.Identificador}] {m.Mensaje}")
                            .ToList();

                        foreach (var mensaje in autorizacion.Mensajes)
                        {
                            _logger.LogWarning("Mensaje SRI: {Mensaje}", mensaje);
                        }

                        stopwatch.Stop();

                        return new ResultadoOperacionSri
                        {
                            Exitoso = false,
                            ClaveAcceso = claveAcceso,
                            Estado = EstadosComprobanteSri.NO_AUTORIZADO,
                            MensajeError = string.Join("; ", mensajesError),
                            Mensajes = autorizacion.Mensajes,
                            NumeroIntentos = intentos,
                            TiempoTranscurrido = stopwatch.Elapsed
                        };
                    }

                    // CASO 3: EN PROCESAMIENTO ⏳
                    if (autorizacion.EnProcesamiento)
                    {
                        _logger.LogInformation("⏳ Comprobante EN PROCESAMIENTO");

                        if (intentos < maxIntentosConsulta)
                        {
                            await EsperarEntreConsultas(intentos, cancellationToken);
                            continue; // Reintentar
                        }
                        else
                        {
                            _logger.LogWarning("⚠️  Máximo de intentos alcanzado, comprobante sigue EN PROCESAMIENTO");

                            stopwatch.Stop();

                            return new ResultadoOperacionSri
                            {
                                Exitoso = false,
                                ClaveAcceso = claveAcceso,
                                Estado = EstadosComprobanteSri.EN_PROCESAMIENTO,
                                MensajeError = "Comprobante sigue en procesamiento después de múltiples intentos",
                                Mensajes = autorizacion.Mensajes,
                                NumeroIntentos = intentos,
                                TiempoTranscurrido = stopwatch.Elapsed
                            };
                        }
                    }

                    // CASO 4: Estado desconocido
                    _logger.LogWarning("⚠️  Estado desconocido: {Estado}", autorizacion.Estado);

                    if (intentos < maxIntentosConsulta)
                    {
                        await EsperarEntreConsultas(intentos, cancellationToken);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogError(ex, "❌ Error en intento {Intento}/{Max}", intentos, maxIntentosConsulta);

                    if (intentos < maxIntentosConsulta)
                    {
                        await EsperarEntreConsultas(intentos, cancellationToken);
                        continue;
                    }
                }
            }

            // Fallaron todos los intentos
            stopwatch.Stop();
            _logger.LogError("❌ FALLO DESPUÉS DE {Intentos} INTENTOS DE CONSULTA", intentos);

            return new ResultadoOperacionSri
            {
                Exitoso = false,
                ClaveAcceso = claveAcceso,
                Estado = "ERROR_CONSULTA",
                MensajeError = $"Error al consultar autorización después de {intentos} intentos: {ultimaExcepcion?.Message}",
                NumeroIntentos = intentos,
                TiempoTranscurrido = stopwatch.Elapsed
            };
        }

        // ============================================================
        // T-078, T-079: FLUJO COMPLETO (ENVIAR + AUTORIZAR)
        // ============================================================

        /// <summary>
        /// T-078, T-079: Ejecuta el flujo completo: Enviar → Esperar → Consultar Autorización
        /// </summary>
        public async Task<ResultadoOperacionSri> EnviarYAutorizarComprobanteAsync(
            string xmlFirmado,
            string claveAcceso,
            string rucEmisor,
            CancellationToken cancellationToken = default)
        {
            var stopwatchTotal = Stopwatch.StartNew();

            _logger.LogInformation("══════════════════════════════════════════════════════════");
            _logger.LogInformation("🚀 INICIANDO FLUJO COMPLETO: ENVÍO + AUTORIZACIÓN");
            _logger.LogInformation("══════════════════════════════════════════════════════════");
            _logger.LogInformation("Clave Acceso: {ClaveAcceso}", claveAcceso);

            try
            {
                // PASO 1: Enviar comprobante (Recepción)
                _logger.LogInformation("📋 PASO 1/3: Enviando comprobante al SRI...");
                var resultadoEnvio = await EnviarComprobanteAsync(
                    xmlFirmado, claveAcceso, rucEmisor, cancellationToken);

                if (!resultadoEnvio.Exitoso)
                {
                    _logger.LogError("❌ Envío falló, no se puede continuar");
                    stopwatchTotal.Stop();
                    resultadoEnvio.TiempoTranscurrido = stopwatchTotal.Elapsed;
                    return resultadoEnvio;
                }

                _logger.LogInformation("✅ PASO 1/3 COMPLETADO - Comprobante recibido por el SRI");

                // PASO 2: Esperar antes de consultar
                var tiempoEspera = _options.EsperaAntesConsultaSegundos;
                _logger.LogInformation("⏳ PASO 2/3: Esperando {Segundos}s antes de consultar autorización...",
                    tiempoEspera);
                await Task.Delay(TimeSpan.FromSeconds(tiempoEspera), cancellationToken);

                // PASO 3: Consultar autorización
                _logger.LogInformation("🔍 PASO 3/3: Consultando autorización...");
                var resultadoAutorizacion = await ConsultarAutorizacionAsync(claveAcceso, cancellationToken);

                stopwatchTotal.Stop();
                resultadoAutorizacion.TiempoTranscurrido = stopwatchTotal.Elapsed;

                if (resultadoAutorizacion.Exitoso)
                {
                    _logger.LogInformation("══════════════════════════════════════════════════════════");
                    _logger.LogInformation("🎉 FLUJO COMPLETADO EXITOSAMENTE");
                    _logger.LogInformation("══════════════════════════════════════════════════════════");
                    _logger.LogInformation("Estado Final: {Estado}", resultadoAutorizacion.Estado);
                    _logger.LogInformation("Número Autorización: {Numero}", resultadoAutorizacion.NumeroAutorizacion);
                    _logger.LogInformation("Tiempo Total: {Tiempo:F2}s", stopwatchTotal.Elapsed.TotalSeconds);
                    _logger.LogInformation("══════════════════════════════════════════════════════════");
                }
                else
                {
                    _logger.LogWarning("══════════════════════════════════════════════════════════");
                    _logger.LogWarning("⚠️  FLUJO COMPLETADO CON ADVERTENCIAS");
                    _logger.LogWarning("══════════════════════════════════════════════════════════");
                    _logger.LogWarning("Estado Final: {Estado}", resultadoAutorizacion.Estado);
                    _logger.LogWarning("Error: {Error}", resultadoAutorizacion.MensajeError);
                    _logger.LogWarning("══════════════════════════════════════════════════════════");
                }

                return resultadoAutorizacion;
            }
            catch (Exception ex)
            {
                stopwatchTotal.Stop();
                _logger.LogError(ex, "❌ Error en flujo completo");

                return new ResultadoOperacionSri
                {
                    Exitoso = false,
                    ClaveAcceso = claveAcceso,
                    Estado = "ERROR_FLUJO",
                    MensajeError = $"Error en flujo completo: {ex.Message}",
                    TiempoTranscurrido = stopwatchTotal.Elapsed
                };
            }
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        /// <summary>
        /// Espera entre consultas según configuración
        /// </summary>
        private async Task EsperarEntreConsultas(int numeroIntento, CancellationToken cancellationToken)
        {
            var delay = _options.CalcularDelay(numeroIntento);
            _logger.LogInformation("⏳ Esperando {Delay}ms antes de reintentar...", delay);
            await Task.Delay(delay, cancellationToken);
        }
    }
}