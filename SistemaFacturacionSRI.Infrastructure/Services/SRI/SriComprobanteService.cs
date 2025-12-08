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
        // SriComprobanteService.cs - VERSIÓN MEJORADA CON DIAGNÓSTICO COMPLETO

        public async Task<ResultadoOperacionSri> EnviarComprobanteAsync(
            string xmlFirmado,
            string claveAcceso,
            string rucEmisor,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var resultado = new ResultadoOperacionSri { ClaveAcceso = claveAcceso };

            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("T-078: INICIANDO ENVÍO DE COMPROBANTE AL SRI");
            _logger.LogInformation("═══════════════════════════════════════════════════════");
            _logger.LogInformation("Clave Acceso: {ClaveAcceso}", claveAcceso);
            _logger.LogInformation("RUC Emisor: {RucEmisor}", rucEmisor);
            _logger.LogInformation("Reintentos máximos: {Max}", _options.ReintentoMaximo);

            // ✅ VALIDACIÓN CRÍTICA: Verificar que el XML tenga firma
            _logger.LogDebug("Verificando presencia de firma electrónica en XML...");

            if (!xmlFirmado.Contains("<Signature") && !xmlFirmado.Contains("ds:Signature"))
            {
                _logger.LogError("❌❌❌ XML NO CONTIENE ELEMENTO <Signature> ❌❌❌");
                _logger.LogError("Tamaño del XML: {Size} caracteres", xmlFirmado.Length);
                _logger.LogError("Primeros 500 caracteres:");
                _logger.LogError("{Xml}", xmlFirmado.Substring(0, Math.Min(500, xmlFirmado.Length)));
                _logger.LogError("Últimos 500 caracteres:");
                var inicio = Math.Max(0, xmlFirmado.Length - 500);
                _logger.LogError("{Xml}", xmlFirmado.Substring(inicio));

                resultado.Exitoso = false;
                resultado.Estado = "ERROR_SIN_FIRMA";
                resultado.MensajeError = "El XML no contiene firma electrónica. No se puede enviar al SRI.";
                resultado.NumeroIntentos = 1;
                resultado.TiempoTranscurrido = stopwatch.Elapsed;
                return resultado;
            }

            if (!xmlFirmado.Contains("SignatureValue") && !xmlFirmado.Contains("ds:SignatureValue"))
            {
                _logger.LogError("❌❌❌ XML NO CONTIENE <SignatureValue> ❌❌❌");
                _logger.LogError("Hay elemento <Signature> pero sin <SignatureValue>");

                resultado.Exitoso = false;
                resultado.Estado = "ERROR_FIRMA_INCOMPLETA";
                resultado.MensajeError = "El XML tiene <Signature> pero sin <SignatureValue>. Firma incompleta.";
                resultado.NumeroIntentos = 1;
                resultado.TiempoTranscurrido = stopwatch.Elapsed;
                return resultado;
            }

            _logger.LogInformation("✅ XML contiene firma electrónica válida");
            _logger.LogDebug("  → Tamaño XML: {Size} bytes", xmlFirmado.Length);

            var intentoActual = 1;
            var maxIntentos = _options.ReintentoMaximo;
            Exception? ultimaExcepcion = null;

            while (intentoActual <= maxIntentos && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("\n🔄 Intento {Actual}/{Max} - Enviando comprobante...",
                        intentoActual, maxIntentos);

                    var request = new RecepcionComprobanteRequest
                    {
                        ClaveAcceso = claveAcceso,
                        RucEmisor = rucEmisor,
                        XmlComprobante = xmlFirmado,
                        FechaEmision = DateTime.Now
                    };

                    var respuesta = await _sriClient.EnviarComprobanteAsync(request, cancellationToken);

                    resultado.NumeroIntentos = intentoActual;

                    // ════════════════════════════════════════════════════════
                    // ANÁLISIS DETALLADO DE LA RESPUESTA
                    // ════════════════════════════════════════════════════════

                    _logger.LogDebug("\n[ANÁLISIS RESPUESTA]");
                    _logger.LogDebug("  Estado raw: '{Estado}'", respuesta.Estado ?? "null");
                    _logger.LogDebug("  FueRecibido: {Recibido}", respuesta.FueRecibido);
                    _logger.LogDebug("  FueDevuelto: {Devuelto}", respuesta.FueDevuelto);
                    _logger.LogDebug("  Comprobantes: {Count}", respuesta.Comprobantes.Count);

                    // CASO 1: RECIBIDO ✅
                    if (respuesta.FueRecibido)
                    {
                        stopwatch.Stop();

                        _logger.LogInformation("\n✅✅✅ Comprobante RECIBIDO por el SRI ✅✅✅");
                        _logger.LogInformation("  → Estado: {Estado}", respuesta.Estado);
                        _logger.LogInformation("  → Tiempo: {Ms}ms", stopwatch.ElapsedMilliseconds);

                        var informativos = respuesta.ObtenerInformativos();
                        foreach (var info in informativos)
                        {
                            _logger.LogInformation("  ℹ️  {Mensaje}", info.Mensaje);
                        }

                        resultado.Exitoso = true;
                        resultado.Estado = EstadosComprobanteSri.RECIBIDA;
                        resultado.TiempoTranscurrido = stopwatch.Elapsed;
                        resultado.Mensajes = respuesta.Comprobantes
    .SelectMany(c => c.Mensajes)
    .ToList(); // Esto mantendrá los objetos de tipo MensajeSri



                        return resultado;
                    }

                    // CASO 2: DEVUELTO ❌
                    else if (respuesta.FueDevuelto)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning("\n❌❌❌ Comprobante DEVUELTO por el SRI ❌❌❌");
                        _logger.LogWarning("  → Estado: {Estado}", respuesta.Estado);
                        _logger.LogWarning("  → Tiempo: {Ms}ms", stopwatch.ElapsedMilliseconds);

                        var errores = respuesta.ObtenerErrores();

                        _logger.LogWarning("\n  Errores reportados por el SRI:");
                        foreach (var error in errores)
                        {
                            var mensajeCompleto = $"[{error.Identificador}] {error.Mensaje}";
                            if (!string.IsNullOrEmpty(error.InformacionAdicional))
                            {
                                mensajeCompleto += $" - {error.InformacionAdicional}";
                            }

                            _logger.LogWarning("    • {Mensaje}", mensajeCompleto);
                          
                        }

                        resultado.Exitoso = false;
                        resultado.Estado = EstadosComprobanteSri.DEVUELTA;
                        resultado.MensajeError = string.Join("; ", resultado.Mensajes);
                        resultado.TiempoTranscurrido = stopwatch.Elapsed;

                        // No reintentar en errores de validación
                        return resultado;
                    }

                    // CASO 3: DESCONOCIDO o VACÍO ⚠️
                    else if (respuesta.Estado == "DESCONOCIDO" || string.IsNullOrEmpty(respuesta.Estado))
                    {
                        _logger.LogWarning("\n⚠️⚠️⚠️ Respuesta DESCONOCIDA del SRI ⚠️⚠️⚠️");
                        _logger.LogWarning("  → Intento: {Intento}/{Max}", intentoActual, maxIntentos);
                        _logger.LogWarning("  → Estado: '{Estado}'", respuesta.Estado ?? "null");

                        // Analizar si hay mensajes en los comprobantes
                        if (respuesta.Comprobantes.Any())
                        {
                            _logger.LogWarning("  → Comprobantes en respuesta: {Count}", respuesta.Comprobantes.Count);

                            foreach (var comp in respuesta.Comprobantes)
                            {
                                _logger.LogDebug("    Comprobante:");
                                _logger.LogDebug("      ClaveAcceso: {Clave}", comp.ClaveAcceso);
                                _logger.LogDebug("      Mensajes: {Count}", comp.Mensajes.Count);

                                foreach (var msg in comp.Mensajes)
                                {
                                    var mensajeCompleto = $"[{msg.Identificador}] {msg.Tipo}: {msg.Mensaje}";
                                    if (!string.IsNullOrEmpty(msg.InformacionAdicional))
                                    {
                                        mensajeCompleto += $" - {msg.InformacionAdicional}";
                                    }

                                    _logger.LogWarning("        {Mensaje}", mensajeCompleto);
                               
                                }
                            }

                            // Si hay mensajes de ERROR, tratarlo como DEVUELTA
                            var hayErrores = respuesta.Comprobantes
                                .SelectMany(c => c.Mensajes)
                                .Any(m => m.Tipo?.ToUpperInvariant() == "ERROR");

                            if (hayErrores)
                            {
                                stopwatch.Stop();

                                _logger.LogWarning("\n❌ Hay mensajes de ERROR - Tratando como DEVUELTA");

                                resultado.Exitoso = false;
                                resultado.Estado = EstadosComprobanteSri.DEVUELTA;
                                resultado.MensajeError = string.Join("; ", resultado.Mensajes);
                                resultado.TiempoTranscurrido = stopwatch.Elapsed;

                                return resultado;
                            }
                        }

                        // Si es el último intento, marcar como ERROR
                        if (intentoActual >= maxIntentos)
                        {
                            stopwatch.Stop();

                            _logger.LogError("\n❌ FALLO: Estado DESCONOCIDO después de {Intentos} intentos", maxIntentos);

                            resultado.Exitoso = false;
                            resultado.Estado = "ERROR_SRI_DESCONOCIDO";
                            resultado.MensajeError = resultado.Mensajes.Any()
                                ? string.Join("; ", resultado.Mensajes)
                                : $"El SRI respondió con estado DESCONOCIDO después de {maxIntentos} intentos. " +
                                  "Esto puede indicar un problema temporal del SRI.";
                            resultado.TiempoTranscurrido = stopwatch.Elapsed;

                            return resultado;
                        }

                        // Esperar antes del siguiente intento
                        var delaySegundos = _options.CalcularDelay(intentoActual) / 1000;
                        _logger.LogInformation("⏳ Esperando {Delay}s antes del siguiente intento...", delaySegundos);
                        await Task.Delay(_options.CalcularDelay(intentoActual), cancellationToken);
                    }

                    // CASO 4: Otro estado no manejado
                    else
                    {
                        _logger.LogWarning("\n⚠️ Estado inesperado: '{Estado}'", respuesta.Estado);

                        if (intentoActual >= maxIntentos)
                        {
                            stopwatch.Stop();

                            resultado.Exitoso = false;
                            resultado.Estado = respuesta.Estado;
                            resultado.MensajeError = $"Estado inesperado del SRI: {respuesta.Estado}";
                            resultado.TiempoTranscurrido = stopwatch.Elapsed;

                            return resultado;
                        }

                        await Task.Delay(_options.CalcularDelay(intentoActual), cancellationToken);
                    }
                }
                catch (TimeoutException ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex, "⏱️  Timeout en intento {Intento}/{Max}", intentoActual, maxIntentos);

                    if (intentoActual < maxIntentos)
                    {
                        var delay = _options.CalcularDelay(intentoActual);
                        _logger.LogInformation("⏳ Esperando {Delay}ms antes del siguiente intento...", delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (HttpRequestException ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex, "🌐 Error de red en intento {Intento}/{Max}", intentoActual, maxIntentos);

                    if (intentoActual < maxIntentos)
                    {
                        var delay = _options.CalcularDelay(intentoActual);
                        _logger.LogInformation("⏳ Esperando {Delay}ms antes del siguiente intento...", delay);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                    _logger.LogError(ex, "❌ Error inesperado en intento {Intento}/{Max}: {Tipo}",
                        intentoActual, maxIntentos, ex.GetType().Name);

                    // No reintentar en errores desconocidos
                    break;
                }

                intentoActual++;
            }

            // Si llegamos aquí, se agotaron los intentos
            stopwatch.Stop();

            _logger.LogError("\n═══════════════════════════════════════════════════════");
            _logger.LogError("❌ FALLO DESPUÉS DE {Intentos} INTENTOS", resultado.NumeroIntentos);
            _logger.LogError("═══════════════════════════════════════════════════════");
            _logger.LogError("Tiempo total: {Tiempo:F2}s", stopwatch.Elapsed.TotalSeconds);

            if (ultimaExcepcion != null)
            {
                _logger.LogError("Último error: {Mensaje}", ultimaExcepcion.Message);
            }

            resultado.Exitoso = false;
            resultado.Estado = "ERROR_MAX_INTENTOS";
            resultado.MensajeError = resultado.MensajeError ??
                $"Error al enviar comprobante después de {resultado.NumeroIntentos} intentos" +
                (ultimaExcepcion != null ? $": {ultimaExcepcion.Message}" : "");
            resultado.TiempoTranscurrido = stopwatch.Elapsed;

            return resultado;
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