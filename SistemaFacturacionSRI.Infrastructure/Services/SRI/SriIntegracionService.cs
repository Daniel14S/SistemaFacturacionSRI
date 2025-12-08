// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriIntegracionService.cs
// T-083: Implementación del orquestador completo - 100% COMPATIBLE
// ✨ MEJORADO: Diagnóstico XML detallado y logging completo

using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.DTOs.Factura;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Infrastructure.Data;
using SistemaFacturacionSRI.Infrastructure.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-083: Orquestador completo de integración con el SRI
    /// ✨ Con diagnóstico XML detallado y logging mejorado
    /// </summary>
    public class SriIntegracionService : ISriIntegracionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirmaElectronicaService _firmaService;
        private readonly SriComprobanteService _sriComprobanteService;
        private readonly ISriWebServiceClient _sriClient;
        private readonly IXmlGeneratorService _xmlGeneratorService; // ✅ AGREGADO
        private readonly ILogger<SriIntegracionService> _logger;
        private readonly ClaveAccesoGenerator _claveAccesoGenerator;

        public SriIntegracionService(
            ApplicationDbContext context,
            IFirmaElectronicaService firmaService,
            SriComprobanteService sriComprobanteService,
            ISriWebServiceClient sriClient,
            IXmlGeneratorService xmlGeneratorService, // ✅ AGREGADO
            ILogger<SriIntegracionService> logger,
            ClaveAccesoGenerator claveAccesoGenerator)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _firmaService = firmaService ?? throw new ArgumentNullException(nameof(firmaService));
            _sriComprobanteService = sriComprobanteService ?? throw new ArgumentNullException(nameof(sriComprobanteService));
            _sriClient = sriClient ?? throw new ArgumentNullException(nameof(sriClient));
            _xmlGeneratorService = xmlGeneratorService ?? throw new ArgumentNullException(nameof(xmlGeneratorService)); // ✅ AGREGADO
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claveAccesoGenerator = claveAccesoGenerator ?? throw new ArgumentNullException(nameof(claveAccesoGenerator));
        }

        // ... [resto del código hasta GenerarXmlFactura] ...

        /// <summary>
        /// ✅ CORREGIDO: Usa XmlGeneratorService en lugar de XML hardcodeado
        /// </summary>
        private string GenerarXmlFactura(Domain.Entities.Factura factura)
        {
            try
            {
                // Mapear entidad de BD a DTO
                var facturaDto = MapearFacturaADto(factura);
                
                // Usar el servicio corregido que genera XML correcto
                return _xmlGeneratorService.GenerarXmlFactura(facturaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar XML para factura {FacturaId}", factura.Id);
                throw;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Mapea la entidad Factura de BD al DTO necesario para generar XML
        /// </summary>
        private FacturaDto MapearFacturaADto(Domain.Entities.Factura factura)
        {
            var facturaDto = new FacturaDto
            {
                NumeroFactura = factura.NumeroFactura,
                ClaveAcceso = factura.ClaveAcceso,
                FechaEmision = factura.FechaEmision == default ? DateTime.Now : factura.FechaEmision,
                Total = factura.ImporteTotal,
                TotalIVA = factura.IVA15,
                
                // Cliente
                Cliente = factura.Cliente != null ? new ClienteFacturaDto
                {
                    TipoIdentificacion = ObtenerCodigoTipoIdentificacion(factura.Cliente.TipoIdentificacionId),
                    Identificacion = factura.Cliente.Identificacion,
                    RazonSocial = factura.Cliente.NombreCompleto(),
                    Direccion = factura.Cliente.Direccion ?? "NO DEFINIDA",
                    Telefono = factura.Cliente.Telefono,
                    Email = factura.Cliente.Email
                } : null,

                // Detalles
                Detalles = factura.Detalles?.Select(d => new DetalleFacturaDto
                {
                    CodigoPrincipal = d.CodigoPrincipal,
                    CodigoAuxiliar = d.CodigoAuxiliar,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    PrecioTotalSinImpuesto = d.PrecioTotalSinImpuesto,
                    
                    // Impuestos
                    CodigoPorcentajeIVA = d.CodigoPorcentajeIVA,
                    Tarifa = d.Tarifa,
                    BaseImponible = d.BaseImponible,
                    Valor = d.Valor
                }).ToList() ?? new List<DetalleFacturaDto>(),

                // Información adicional (si existe)
                InfoAdicional = new List<InfoAdicionalDto>()
            };

            return facturaDto;
        }

        /// <summary>
        /// ✅ NUEVO: Obtiene el código SRI del tipo de identificación
        /// </summary>
        private string ObtenerCodigoTipoIdentificacion(int tipoIdentificacionId)
        {
            // Mapeo de IDs de BD a códigos SRI
            return tipoIdentificacionId switch
            {
                1 => "04", // RUC
                2 => "05", // Cédula
                3 => "06", // Pasaporte
                4 => "07", // Consumidor Final
                5 => "08", // Identificación del exterior
                _ => "07"  // Por defecto: Consumidor Final
            };
        }

        // ============================================================
        // T-083: PROCESAR FACTURA COMPLETA CON LOGGING DETALLADO
        // ============================================================
        public async Task<ResultadoIntegracionSri> ProcesarFacturaCompletaAsync(
            int facturaId,
            CancellationToken cancellationToken = default)
        {
            var stopwatchTotal = Stopwatch.StartNew();
            var resultado = new ResultadoIntegracionSri { FacturaId = facturaId };

            _logger.LogInformation("\n");
            _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║                                                                ║");
            _logger.LogInformation("║     🚀 PROCESAMIENTO COMPLETO DE FACTURA - INICIO             ║");
            _logger.LogInformation("║                                                                ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("  → ID Factura: #{Id}", facturaId);
            _logger.LogInformation("  → Fecha/Hora: {Fecha}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                // ═══════════════════════════════════════════════════════
                // ETAPA 1: CARGAR FACTURA DE LA BD
                // ═══════════════════════════════════════════════════════
                var swEtapa1 = Stopwatch.StartNew();
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  ETAPA 1/6: Cargando factura de la base de datos              ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

                var factura = await _context.Facturas
                    .Include(f => f.Cliente)
                    .Include(f => f.Detalles)
                        .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(f => f.Id == facturaId, cancellationToken);

                if (factura == null)
                {
                    swEtapa1.Stop();
                    resultado.AgregarEtapa("CargarFactura", false, swEtapa1.Elapsed, "Factura no encontrada");
                    resultado.MensajeError = "Factura no encontrada";
                    _logger.LogError("  ✗ Error: Factura #{Id} no encontrada en la base de datos", facturaId);
                    return resultado;
                }

                swEtapa1.Stop();
                resultado.AgregarEtapa("CargarFactura", true, swEtapa1.Elapsed);

                _logger.LogInformation("  ✓ Factura cargada exitosamente");
                _logger.LogInformation("  → Número: {Numero}", factura.NumeroFactura);
                _logger.LogInformation("  → Estado actual: {Estado}", factura.Estado);
                _logger.LogInformation("  → Cliente: {Cliente}", factura.Cliente?.NombreCompleto() ?? "N/A");
                _logger.LogInformation("  → Total: ${Total:F2}", factura.ImporteTotal);
                _logger.LogInformation("  → Detalles: {Count} items", factura.Detalles.Count);
                _logger.LogInformation("  ✓ ETAPA 1/6 COMPLETADA en {Ms}ms\n", swEtapa1.ElapsedMilliseconds);

                // Validar y generar clave de acceso
                try
                {
                    resultado.ClaveAcceso = await AsegurarClaveAccesoValidaAsync(factura, cancellationToken);
                    _logger.LogInformation("  ✓ Clave de acceso validada: {Clave}", resultado.ClaveAcceso);
                }
                catch (Exception ex)
                {
                    resultado.MensajeError = $"No se pudo obtener una clave de acceso válida: {ex.Message}";
                    resultado.EstadoFinal = "ERROR_CLAVE_ACCESO";
                    _logger.LogError("  ✗ Error al validar clave de acceso: {Message}", ex.Message);
                    return resultado;
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 2: CARGAR XML FIRMADO EXISTENTE O GENERAR NUEVO
                // ✅ CORRECCIÓN: Verificar que los archivos existen Y la clave coincide
                // ============================================================
                // 🔥 CORRECCIÓN CRÍTICA: SriIntegracionService.cs
                // Ubicación: Método ProcesarFacturaCompletaAsync
                // Línea aproximada: 80-120 (después de cargar la factura)
                // ============================================================

                // REEMPLAZAR TODO EL BLOQUE DE "ETAPA 2" con este código corregido:

                // ═══════════════════════════════════════════════════════
                // ETAPA 2: GENERAR/CARGAR XML CON CLAVE CORRECTA
                // ✅ CORRECCIÓN: Siempre regenerar clave antes de XML
                // ═══════════════════════════════════════════════════════
                var swEtapa2 = Stopwatch.StartNew();
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  ETAPA 2/6: Generando XML con clave de acceso correcta        ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

                string xmlFirmado = string.Empty;

                try
                {
                    // ✅ PASO 1: REGENERAR LA CLAVE DE ACCESO SIEMPRE
                    _logger.LogInformation("  → PASO 1/3: Regenerando clave de acceso...");

                    var config = await _context.ConfiguracionEmpresa
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cancellationToken);

                    if (config == null)
                    {
                        throw new InvalidOperationException("No hay configuración de empresa");
                    }

                    // Extraer componentes del número de factura
                    var partes = factura.NumeroFactura.Split('-');
                    if (partes.Length != 3)
                    {
                        throw new InvalidOperationException(
                            $"Formato de número de factura inválido: {factura.NumeroFactura}");
                    }

                    var establecimiento = partes[0].Trim().PadLeft(3, '0');
                    var puntoEmision = partes[1].Trim().PadLeft(3, '0');
                    var secuencial = partes[2].Trim().PadLeft(9, '0');
                    var tipoEmision = string.IsNullOrWhiteSpace(config.TipoEmision) ? "1" : config.TipoEmision;

                    // Validar TipoEmision
                    if (tipoEmision != "1" && tipoEmision != "2")
                    {
                        _logger.LogWarning("  ⚠️ TipoEmision '{TipoEmision}' inválido. Usando '1'", tipoEmision);
                        tipoEmision = "1";
                    }

                    _logger.LogInformation("  → Datos para clave:");
                    _logger.LogInformation("    • Fecha: {Fecha:dd/MM/yyyy}", factura.FechaEmision);
                    _logger.LogInformation("    • RUC: {Ruc}", config.RUC);
                    _logger.LogInformation("    • Ambiente: {Ambiente}", config.AmbienteSRI);
                    _logger.LogInformation("    • Tipo Emisión: {TE}", tipoEmision);
                    _logger.LogInformation("    • Establecimiento: {Est}", establecimiento);
                    _logger.LogInformation("    • Punto Emisión: {Pto}", puntoEmision);
                    _logger.LogInformation("    • Secuencial: {Sec}", secuencial);

                    // Generar nueva clave
                    var claveNueva = _claveAccesoGenerator.GenerarClaveAcceso(
                        factura.FechaEmision == default ? DateTime.Now : factura.FechaEmision,
                        "01", // Factura
                        config.RUC,
                        config.AmbienteSRI,
                        tipoEmision,
                        establecimiento,
                        puntoEmision,
                        secuencial
                    );

                    // Validar la nueva clave
                    if (!_claveAccesoGenerator.ValidarClaveAcceso(claveNueva))
                    {
                        throw new InvalidOperationException($"Clave generada inválida: {claveNueva}");
                    }

                    _logger.LogInformation("  ✓ Nueva clave generada: {Clave}", claveNueva);

                    // Si cambió la clave, actualizar BD y limpiar XMLs
                    if (factura.ClaveAcceso != claveNueva)
                    {
                        _logger.LogWarning("  ⚠️ Clave cambió. Actualizando BD...");
                        _logger.LogInformation("    Anterior: {Old}", factura.ClaveAcceso ?? "NINGUNA");
                        _logger.LogInformation("    Nueva:    {New}", claveNueva);

                        factura.ClaveAcceso = claveNueva;
                        factura.XmlPath = null;
                        factura.XmlFirmadoPath = null;

                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("  ✓ Clave actualizada en BD");
                    }

                    resultado.ClaveAcceso = claveNueva;

                    // ✅ PASO 2/3: GENERAR Y FIRMAR XML CON LA CLAVE CORRECTA
                    _logger.LogInformation("\n  → PASO 2/3: Generando y firmando XML...");

                    xmlFirmado = await GenerarYFirmarXmlAsync(factura, cancellationToken);

                    _logger.LogInformation("  ✓ XML generado y firmado correctamente");
                    _logger.LogInformation("  → Tamaño: {Size} bytes", xmlFirmado.Length);

                    // ✅ PASO 3/3: VERIFICAR QUE LA CLAVE EN EL XML COINCIDA
                    _logger.LogInformation("\n  → PASO 3/3: Verificando coherencia...");

                    var docVerif = XDocument.Parse(xmlFirmado);
                    var claveEnXml = docVerif.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName == "claveAcceso")?.Value;

                    if (claveEnXml != resultado.ClaveAcceso)
                    {
                        _logger.LogError("  ❌ CLAVE NO COINCIDE:");
                        _logger.LogError("     XML tiene:    {XmlClave}", claveEnXml);
                        _logger.LogError("     Debe tener:   {ExpectedClave}", resultado.ClaveAcceso);

                        throw new InvalidOperationException(
                            $"La clave en el XML ({claveEnXml}) no coincide con la esperada ({resultado.ClaveAcceso})");
                    }

                    _logger.LogInformation("  ✅ Clave en XML coincide: {Clave}", claveEnXml);

                    // ✨ DIAGNÓSTICO COMPLETO DEL XML
                    DiagnosticarXml(xmlFirmado, resultado.ClaveAcceso);

                    resultado.XmlFirmado = xmlFirmado;

                    swEtapa2.Stop();
                    resultado.AgregarEtapa("GenerarYFirmarXml", true, swEtapa2.Elapsed);
                    _logger.LogInformation("  ✓ ETAPA 2/6 COMPLETADA en {Ms}ms\n", swEtapa2.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    swEtapa2.Stop();
                    resultado.AgregarEtapa("GenerarYFirmarXml", false, swEtapa2.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al generar XML: {ex.Message}";
                    _logger.LogError("  ✗ Error en ETAPA 2: {Message}", ex.Message);
                    return resultado;
                }

                // ═══════════════════════════════════════════════════════
                // CONTINÚA CON ETAPA 4 (ya no hay ETAPA 3)
                // ═══════════════════════════════════════════════════════
                // ═══════════════════════════════════════════════════════
                // ETAPA 4: ENVIAR AL SRI (RECEPCIÓN)
                // ═══════════════════════════════════════════════════════
                var swEtapa4 = Stopwatch.StartNew();
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  ETAPA 4/6: Enviando comprobante al SRI (Recepción)           ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

                ResultadoOperacionSri resultadoEnvio;
                try
                {
                    var configEmpresa = await _context.ConfiguracionEmpresa
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cancellationToken);

                    if (configEmpresa == null || string.IsNullOrWhiteSpace(configEmpresa.RUC) || configEmpresa.RUC.Length != 13)
                    {
                        throw new InvalidOperationException("No se encontró un RUC válido de la empresa emisora (se requieren 13 dígitos).");
                    }

                    string rucEmpresa = configEmpresa.RUC;

                    _logger.LogInformation("  → RUC Emisor: {Ruc}", rucEmpresa);
                    _logger.LogInformation("  → Clave Acceso: {Clave}", resultado.ClaveAcceso);
                    _logger.LogInformation("\n  → Iniciando envío al SRI...");

                    resultadoEnvio = await _sriComprobanteService.EnviarComprobanteAsync(
                        xmlFirmado,
                        resultado.ClaveAcceso,
                        rucEmpresa,
                        cancellationToken);

                    swEtapa4.Stop();
                    resultado.TotalIntentos += resultadoEnvio.NumeroIntentos;
                    resultado.Mensajes.AddRange(resultadoEnvio.Mensajes);

                    if (resultadoEnvio.Exitoso)
                    {
                        resultado.AgregarEtapa("EnviarSri", true, swEtapa4.Elapsed);
                        _logger.LogInformation("\n  ✅ Comprobante RECIBIDO por el SRI");
                        _logger.LogInformation("  → Total intentos: {Intentos}", resultadoEnvio.NumeroIntentos);
                        _logger.LogInformation("  ✓ ETAPA 4/6 COMPLETADA en {Ms}ms\n", swEtapa4.ElapsedMilliseconds);
                    }
                    else
                    {
                        resultado.AgregarEtapa("EnviarSri", false, swEtapa4.Elapsed, resultadoEnvio.MensajeError);
                        resultado.MensajeError = resultadoEnvio.MensajeError;
                        resultado.EstadoFinal = "DEVUELTA";

                        _logger.LogWarning("\n  ❌ Comprobante DEVUELTO por el SRI");
                        _logger.LogWarning("  → Motivo: {Mensaje}", resultadoEnvio.MensajeError);

                        foreach (var msg in resultadoEnvio.Mensajes)
                        {
                            _logger.LogWarning("  → {Mensaje}", msg);
                        }

                        await ActualizarEstadoFacturaAsync(
                            factura,
                            EstadoFactura.DEVUELTA,
                            null,
                            null,
                            resultadoEnvio.MensajeError,
                            cancellationToken);
                        return resultado;
                    }
                }
                catch (Exception ex)
                {
                    swEtapa4.Stop();
                    resultado.AgregarEtapa("EnviarSri", false, swEtapa4.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al enviar al SRI: {ex.Message}";

                    _logger.LogError("\n  ✗ Error al enviar al SRI: {Message}", ex.Message);

                    await ActualizarEstadoFacturaAsync(
                        factura,
                        EstadoFactura.FIRMADA,
                        null,
                        null,
                        ex.Message,
                        cancellationToken);
                    return resultado;
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 5: ESPERAR Y CONSULTAR AUTORIZACIÓN
                // ═══════════════════════════════════════════════════════
                var swEtapa5 = Stopwatch.StartNew();
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  ETAPA 5/6: Consultando autorización en el SRI                ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

                _logger.LogInformation("  ⏳ Esperando 5 segundos antes de consultar autorización...");
                await Task.Delay(5000, cancellationToken);

                ResultadoOperacionSri resultadoAutorizacion;
                try
                {
                    _logger.LogInformation("  → Consultando autorización con reintentos automáticos...");

                    resultadoAutorizacion = await _sriComprobanteService.ConsultarAutorizacionAsync(
                        resultado.ClaveAcceso,
                        cancellationToken);

                    swEtapa5.Stop();
                    resultado.TotalIntentos += resultadoAutorizacion.NumeroIntentos;
                    resultado.Mensajes.AddRange(resultadoAutorizacion.Mensajes);

                    if (resultadoAutorizacion.Exitoso)
                    {
                        resultado.AgregarEtapa("ConsultarAutorizacion", true, swEtapa5.Elapsed);
                        resultado.Exitoso = true;
                        resultado.EstadoFinal = "AUTORIZADO";
                        resultado.NumeroAutorizacion = resultadoAutorizacion.NumeroAutorizacion;
                        resultado.FechaAutorizacion = resultadoAutorizacion.FechaAutorizacion;
                        resultado.XmlAutorizado = resultadoAutorizacion.XmlAutorizado;

                        _logger.LogInformation("\n  🎉 Comprobante AUTORIZADO exitosamente");
                        _logger.LogInformation("  → Número Autorización: {Numero}", resultado.NumeroAutorizacion);
                        _logger.LogInformation("  → Fecha Autorización: {Fecha}", resultado.FechaAutorizacion);
                        _logger.LogInformation("  → Total intentos: {Intentos}", resultadoAutorizacion.NumeroIntentos);
                        _logger.LogInformation("  → Tamaño XML Autorizado: {Size} bytes",
                            resultado.XmlAutorizado?.Length ?? 0);
                        _logger.LogInformation("  ✓ ETAPA 5/6 COMPLETADA en {Ms}ms\n", swEtapa5.ElapsedMilliseconds);
                    }
                    else
                    {
                        resultado.AgregarEtapa("ConsultarAutorizacion", false, swEtapa5.Elapsed, resultadoAutorizacion.MensajeError);
                        resultado.EstadoFinal = resultadoAutorizacion.Estado;
                        resultado.MensajeError = resultadoAutorizacion.MensajeError;

                        _logger.LogWarning("\n  ⚠️ Comprobante NO autorizado");
                        _logger.LogWarning("  → Estado: {Estado}", resultado.EstadoFinal);
                        _logger.LogWarning("  → Motivo: {Mensaje}", resultado.MensajeError);

                        foreach (var msg in resultadoAutorizacion.Mensajes)
                        {
                            _logger.LogWarning("  → {Mensaje}", msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    swEtapa5.Stop();
                    resultado.AgregarEtapa("ConsultarAutorizacion", false, swEtapa5.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al consultar autorización: {ex.Message}";
                    resultado.EstadoFinal = "ERROR_CONSULTA";

                    _logger.LogError("\n  ✗ Error al consultar autorización: {Message}", ex.Message);
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 6: ACTUALIZAR ESTADO EN BASE DE DATOS
                // ═══════════════════════════════════════════════════════
                var swEtapa6 = Stopwatch.StartNew();
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  ETAPA 6/6: Actualizando estado en base de datos              ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

                try
                {
                    var estadoEnum = MapearEstadoStringAEnum(resultado.EstadoFinal);

                    _logger.LogInformation("  → Estado final: {Estado}", estadoEnum);

                    await ActualizarEstadoFacturaAsync(
                        factura,
                        estadoEnum,
                        resultado.NumeroAutorizacion,
                        resultado.FechaAutorizacion,
                        resultado.MensajeError,
                        cancellationToken);

                    resultado.EstadoActualizadoEnBd = true;

                    swEtapa6.Stop();
                    resultado.AgregarEtapa("ActualizarBD", true, swEtapa6.Elapsed);
                    _logger.LogInformation("  ✓ Estado actualizado correctamente en BD");
                    _logger.LogInformation("  ✓ ETAPA 6/6 COMPLETADA en {Ms}ms\n", swEtapa6.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    swEtapa6.Stop();
                    resultado.AgregarEtapa("ActualizarBD", false, swEtapa6.Elapsed, ex.Message);
                    _logger.LogError("  ✗ Error al actualizar estado en BD: {Message}", ex.Message);
                }

                stopwatchTotal.Stop();
                resultado.TiempoTotal = stopwatchTotal.Elapsed;

                // ═══════════════════════════════════════════════════════
                // RESUMEN FINAL DETALLADO
                // ═══════════════════════════════════════════════════════
                ImprimirResumenFinal(resultado, facturaId);

                return resultado;
            }
            catch (Exception ex)
            {
                stopwatchTotal.Stop();
                _logger.LogError("\n╔════════════════════════════════════════════════════════════════╗");
                _logger.LogError("║              ERROR CRÍTICO EN PROCESAMIENTO                    ║");
                _logger.LogError("╚════════════════════════════════════════════════════════════════╝");
                _logger.LogError("  → Factura: #{Id}", facturaId);
                _logger.LogError("  → Tipo: {Type}", ex.GetType().Name);
                _logger.LogError("  → Mensaje: {Message}", ex.Message);
                _logger.LogError("  → StackTrace: {Stack}", ex.StackTrace);

                resultado.Exitoso = false;
                resultado.EstadoFinal = "ERROR_CRITICO";
                resultado.MensajeError = $"Error crítico: {ex.Message}";
                resultado.TiempoTotal = stopwatchTotal.Elapsed;

                return resultado;
            }
        }

        // ============================================================
        // ✨ DIAGNÓSTICO COMPLETO DEL XML (del archivo 3)
        // ============================================================

        /// <summary>
        /// Diagnóstico completo del XML antes de enviar al SRI
        /// </summary>
        private void DiagnosticarXml(string xml, string claveAcceso)
        {
            _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║              DIAGNÓSTICO COMPLETO DEL XML                      ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

            try
            {
                // ✅ VERIFICACIÓN CRÍTICA: Debe tener declaración XML con UTF-8 mayúsculas
                var xmlTrimmed = xml.TrimStart();

                if (!xmlTrimmed.StartsWith("<?xml"))
                {
                    _logger.LogError("  ❌❌❌ XML NO TIENE DECLARACIÓN - EL SRI LO RECHAZARÁ ❌❌❌");
                    _logger.LogError("  → Debe comenzar con: <?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                }
                else
                {
                    var declaracion = xml.Substring(0, Math.Min(100, xml.Length));
                    _logger.LogInformation("  ✓ XML tiene declaración: {Declaracion}", declaracion);

                    // ✅ Verificar el encoding específicamente
                    if (declaracion.Contains("encoding=\"UTF-8\""))
                    {
                        _logger.LogInformation("  ✅ ENCODING CORRECTO: UTF-8 (mayúsculas) ✅");
                    }
                    else if (declaracion.Contains("encoding=\"utf-8\""))
                    {
                        _logger.LogError("  ❌ ENCODING INCORRECTO: utf-8 (minúsculas)");
                        _logger.LogError("  → El SRI puede rechazar el comprobante");
                        _logger.LogError("  → DEBE ser: encoding=\"UTF-8\" (mayúsculas)");
                    }
                    else
                    {
                        _logger.LogWarning("  ⚠️ No se detectó atributo encoding en la declaración");
                    }
                }

                var doc = XDocument.Parse(xml);

                _logger.LogInformation("  ✓ XML válido (parseado correctamente)");
                _logger.LogInformation("  → Encoding declarado en objeto: {Encoding}",
                    doc.Declaration?.Encoding ?? "ninguno");

                var root = doc.Root;
                _logger.LogInformation("  → Elemento raíz: {Root}", root?.Name.LocalName);
                _logger.LogInformation("  → Atributo 'id': {Id}", root?.Attribute("id")?.Value ?? "NO ENCONTRADO");

                if (root?.Attribute("id")?.Value != "comprobante")
                {
                    _logger.LogWarning("  ⚠️ ADVERTENCIA: El atributo 'id' debe ser 'comprobante'");
                }

                // Verificar infoTributaria
                var infoTrib = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infoTributaria");
                if (infoTrib != null)
                {
                    _logger.LogInformation("\n  ✓ infoTributaria encontrado");
                    _logger.LogInformation("    → Ambiente: {Ambiente}", infoTrib.Element("ambiente")?.Value ?? "NO ENCONTRADO");
                    _logger.LogInformation("    → Tipo Emisión: {TipoEmision}", infoTrib.Element("tipoEmision")?.Value ?? "NO ENCONTRADO");
                    _logger.LogInformation("    → RUC: {Ruc}", infoTrib.Element("ruc")?.Value ?? "NO ENCONTRADO");
                    _logger.LogInformation("    → Establecimiento: {Estab}", infoTrib.Element("estab")?.Value ?? "NO ENCONTRADO");
                    _logger.LogInformation("    → Punto Emisión: {Punto}", infoTrib.Element("ptoEmi")?.Value ?? "NO ENCONTRADO");
                    _logger.LogInformation("    → Secuencial: {Sec}", infoTrib.Element("secuencial")?.Value ?? "NO ENCONTRADO");

                    var claveEnXml = infoTrib.Element("claveAcceso")?.Value;
                    _logger.LogInformation("    → Clave Acceso XML: {Clave}", claveEnXml ?? "NO ENCONTRADO");

                    if (claveEnXml != claveAcceso)
                    {
                        _logger.LogWarning("    ⚠️ ADVERTENCIA: Clave en XML ({ClaveXml}) no coincide con la esperada ({ClaveEsperada})",
                            claveEnXml, claveAcceso);
                    }
                    else
                    {
                        _logger.LogInformation("    ✓ Clave de acceso coincide");
                    }
                }
                else
                {
                    _logger.LogError("  ✗✗✗ NO SE ENCONTRÓ infoTributaria - ERROR CRÍTICO ✗✗✗");
                }

                // Verificar firma
                var signature = doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Signature");

                if (signature != null)
                {
                    _logger.LogInformation("\n  ✓ Firma electrónica presente");
                }
                else
                {
                    _logger.LogError("  ✗✗✗ NO SE ENCONTRÓ firma electrónica (Signature) - ERROR CRÍTICO ✗✗✗");
                }

                _logger.LogInformation("\n  → Primeros 200 caracteres del XML:");
                _logger.LogInformation("    {Inicio}", xml.Substring(0, Math.Min(200, xml.Length)));

                _logger.LogInformation("\n  → Últimos 200 caracteres del XML:");
                var inicio = Math.Max(0, xml.Length - 200);
                _logger.LogInformation("    ...{Final}", xml.Substring(inicio));

            }
            catch (XmlException ex)
            {
                _logger.LogError("  ✗✗✗ XML INVÁLIDO ✗✗✗");
                _logger.LogError("    Error: {Message}", ex.Message);
                _logger.LogError("    Línea: {Line}, Posición: {Position}", ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                _logger.LogError("  ✗ Error al diagnosticar: {Message}", ex.Message);
            }

            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝\n");
        }

        // ============================================================
        // IMPRIMIR RESUMEN FINAL
        // ============================================================

        private void ImprimirResumenFinal(ResultadoIntegracionSri resultado, int facturaId)
        {
            _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║                                                                ║");

            if (resultado.Exitoso)
            {
                _logger.LogInformation("║     🎉 PROCESAMIENTO COMPLETADO EXITOSAMENTE                  ║");
            }
            else
            {
                _logger.LogInformation("║     ⚠️ PROCESAMIENTO COMPLETADO CON ERRORES                   ║");
            }

            _logger.LogInformation("║                                                                ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("  → Factura: #{Id}", facturaId);
            _logger.LogInformation("  → Estado Final: {Estado}", resultado.EstadoFinal);
            _logger.LogInformation("  → Tiempo Total: {Tiempo:F2}s", resultado.TiempoTotal.TotalSeconds);
            _logger.LogInformation("  → Total Intentos: {Intentos}", resultado.TotalIntentos);

            if (!string.IsNullOrEmpty(resultado.NumeroAutorizacion))
            {
                _logger.LogInformation("  → Número Autorización: {Numero}", resultado.NumeroAutorizacion);
                _logger.LogInformation("  → Fecha Autorización: {Fecha}", resultado.FechaAutorizacion);
            }

            if (!string.IsNullOrEmpty(resultado.MensajeError))
            {
                _logger.LogWarning("  → Error: {Error}", resultado.MensajeError);
            }

         

            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝\n");
        }

        // ============================================================
        // T-083: PROCESAR MÚLTIPLES FACTURAS
        // ============================================================

        public async Task<List<ResultadoIntegracionSri>> ProcesarFacturasLoteAsync(
            List<int> facturasIds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║           PROCESAMIENTO EN LOTE                                ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("  → Total facturas: {Count}", facturasIds.Count);

            var resultados = new List<ResultadoIntegracionSri>();
            int contador = 0;

            foreach (var facturaId in facturasIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("  ⚠️ Procesamiento en lote cancelado");
                    break;
                }

                contador++;
                _logger.LogInformation("\n  → Procesando factura {Actual}/{Total}...", contador, facturasIds.Count);

                var resultado = await ProcesarFacturaCompletaAsync(facturaId, cancellationToken);
                resultados.Add(resultado);

                // Pequeña pausa entre facturas para no saturar el SRI
                await Task.Delay(1000, cancellationToken);
            }

            var exitosas = resultados.Count(r => r.Exitoso);
            var fallidas = resultados.Count - exitosas;

            _logger.LogInformation("\n╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║              RESUMEN PROCESAMIENTO LOTE                        ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
            _logger.LogInformation("  → Total procesadas: {Total}", resultados.Count);
            _logger.LogInformation("  → Exitosas: {Exitosas} ✓", exitosas);
            _logger.LogInformation("  → Fallidas: {Fallidas} ✗", fallidas);
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝\n");

            return resultados;
        }

        // ============================================================
        // T-083: REPROCESAR FACTURA
        // ============================================================

        public async Task<ResultadoIntegracionSri> ReprocesarFacturaAsync(
            int facturaId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔄 REPROCESANDO factura #{Id}", facturaId);
            return await ProcesarFacturaCompletaAsync(facturaId, cancellationToken);
        }

        // ============================================================
        // T-083: CONSULTAR ESTADO
        // ============================================================

        public async Task<ResultadoIntegracionSri> ConsultarEstadoFacturaAsync(
            int facturaId,
            CancellationToken cancellationToken = default)
        {
            var resultado = new ResultadoIntegracionSri { FacturaId = facturaId };

            try
            {
                var factura = await _context.Facturas.FindAsync(facturaId);

                if (factura == null)
                {
                    resultado.MensajeError = "Factura no encontrada";
                    return resultado;
                }

                if (string.IsNullOrEmpty(factura.ClaveAcceso))
                {
                    resultado.MensajeError = "Factura no tiene clave de acceso";
                    return resultado;
                }

                resultado.ClaveAcceso = factura.ClaveAcceso;

                var resultadoConsulta = await _sriComprobanteService.ConsultarAutorizacionAsync(
                    factura.ClaveAcceso,
                    cancellationToken);

                resultado.Exitoso = resultadoConsulta.Exitoso;
                resultado.EstadoFinal = resultadoConsulta.Estado;
                resultado.NumeroAutorizacion = resultadoConsulta.NumeroAutorizacion;
                resultado.FechaAutorizacion = resultadoConsulta.FechaAutorizacion;
                resultado.Mensajes = resultadoConsulta.Mensajes;

                if (resultado.Exitoso)
                {
                    var estadoEnum = MapearEstadoStringAEnum(resultado.EstadoFinal);
                    await ActualizarEstadoFacturaAsync(
                        factura,
                        estadoEnum,
                        resultado.NumeroAutorizacion,
                        resultado.FechaAutorizacion,
                        null,
                        cancellationToken);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar estado de factura #{Id}", facturaId);
                resultado.MensajeError = ex.Message;
                return resultado;
            }
        }

        public async Task<EstadoServiciosSri> VerificarEstadoSriAsync()
        {
            return await _sriClient.ObtenerEstadoServiciosAsync();
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        private async Task ActualizarEstadoFacturaAsync(
            Domain.Entities.Factura factura,
            EstadoFactura estado,
            string? numeroAutorizacion,
            DateTime? fechaAutorizacion,
            string? mensajeError,
            CancellationToken cancellationToken)
        {
            factura.Estado = estado;
            factura.NumeroAutorizacion = numeroAutorizacion;
            factura.FechaHoraAutorizacion = fechaAutorizacion;
            factura.MensajesSRI = mensajeError;
            factura.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("  → Estado actualizado en BD: {Estado}", estado);
        }

        private EstadoFactura MapearEstadoStringAEnum(string estado)
        {
            return estado?.ToUpperInvariant() switch
            {
                "AUTORIZADO" => EstadoFactura.AUTORIZADA,
                "DEVUELTA" => EstadoFactura.DEVUELTA,
                "NO_AUTORIZADO" => EstadoFactura.NO_AUTORIZADA,
                "RECIBIDA" => EstadoFactura.RECIBIDA,
                "ENVIADA" => EstadoFactura.ENVIADA,
                "ERROR_FIRMA" => EstadoFactura.BORRADOR,
                "ERROR_ENVIO" => EstadoFactura.FIRMADA,
                _ => EstadoFactura.BORRADOR
            };
        }

        // ============================================================
        // ✅ CORRECCIÓN COMPLETA DEL MÉTODO AsegurarClaveAccesoValidaAsync
        // REEMPLAZAR EN: SriIntegracionService.cs (línea ~689)
        // ============================================================

        private async Task<string> AsegurarClaveAccesoValidaAsync(
            Domain.Entities.Factura factura,
            CancellationToken cancellationToken)
        {
            var claveActual = factura.ClaveAcceso?.Trim();

            // 1. Si ya tiene una clave válida, usarla
            if (!string.IsNullOrWhiteSpace(claveActual) &&
                claveActual.Length == 49 &&
                _claveAccesoGenerator.ValidarClaveAcceso(claveActual))
            {
                _logger.LogInformation("  ✓ Clave de acceso existente es válida: {Clave}", claveActual);
                return claveActual;
            }

            _logger.LogWarning("  ⚠️ Clave de acceso inválida o inexistente. Regenerando...");

            // 2. Obtener configuración
            var config = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null)
            {
                throw new InvalidOperationException("No hay configuración de empresa para recalcular la clave de acceso.");
            }

            // 3. Validar número de factura
            if (string.IsNullOrWhiteSpace(factura.NumeroFactura))
            {
                throw new InvalidOperationException("La factura no tiene número de factura para reconstruir la clave de acceso.");
            }

            // 4. ✅ EXTRAER CORRECTAMENTE del número de factura
            var partes = factura.NumeroFactura.Split('-');
            if (partes.Length != 3)
            {
                throw new InvalidOperationException(
                    $"El número de factura '{factura.NumeroFactura}' no tiene el formato esperado 'xxx-xxx-xxxxxxxxx'.");
            }

            // ✅ CRÍTICO: Asegurar formato correcto con padding
            var establecimiento = partes[0].Trim().PadLeft(3, '0');
            var puntoEmision = partes[1].Trim().PadLeft(3, '0');
            var secuencial = partes[2].Trim().PadLeft(9, '0');

            // ✅ CRÍTICO: Usar tipo de emisión correcto (1 = Normal, 2 = Indisponibilidad)
            var tipoEmision = string.IsNullOrWhiteSpace(config.TipoEmision) ? "1" : config.TipoEmision;

            // ✅ Validar que TipoEmision sea 1 o 2
            if (tipoEmision != "1" && tipoEmision != "2")
            {
                _logger.LogWarning("  ⚠️ TipoEmision '{TipoEmision}' inválido. Usando '1' (Normal)", tipoEmision);
                tipoEmision = "1";
            }

            _logger.LogInformation("  → Datos para regenerar clave:");
            _logger.LogInformation("    • Fecha Emisión: {Fecha:dd/MM/yyyy}", factura.FechaEmision);
            _logger.LogInformation("    • Tipo Comprobante: 01 (Factura)");
            _logger.LogInformation("    • RUC: {Ruc}", config.RUC);
            _logger.LogInformation("    • Ambiente: {Ambiente}", config.AmbienteSRI);
            _logger.LogInformation("    • Tipo Emisión: {TipoEmision}", tipoEmision);
            _logger.LogInformation("    • Establecimiento: {Estab}", establecimiento);
            _logger.LogInformation("    • Punto Emisión: {Punto}", puntoEmision);
            _logger.LogInformation("    • Secuencial: {Sec}", secuencial);

            // 5. Generar nueva clave de acceso
            var claveNueva = _claveAccesoGenerator.GenerarClaveAcceso(
                factura.FechaEmision == default ? DateTime.Now : factura.FechaEmision,
                "01", // Factura
                config.RUC,
                config.AmbienteSRI,
                tipoEmision,
                establecimiento,
                puntoEmision,
                secuencial
            );

            // 6. Validar la nueva clave
            if (!_claveAccesoGenerator.ValidarClaveAcceso(claveNueva))
            {
                throw new InvalidOperationException($"La clave de acceso regenerada es inválida: {claveNueva}");
            }

            // 7. Actualizar en la base de datos
            var claveAnterior = factura.ClaveAcceso;
            factura.ClaveAcceso = claveNueva;

            // ✅ ✅ ✅ CRÍTICO: SI CAMBIAMOS LA CLAVE, LIMPIAR LOS XMLs ANTERIORES
            // Esto forzará la regeneración completa en ETAPA 2
            if (claveAnterior != claveNueva)
            {
                _logger.LogWarning("  ⚠️ Clave de acceso cambió. Limpiando XMLs anteriores para regeneración...");

                // Limpiar rutas de archivos antiguos (si existen)
                factura.XmlPath = null;
                factura.XmlFirmadoPath = null;

                _logger.LogInformation("  → XMLs anteriores marcados para regeneración");
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("  ✓ Clave de acceso regenerada exitosamente");
            _logger.LogInformation("    Anterior: {ClaveAnterior}", claveAnterior ?? "NINGUNA");
            _logger.LogInformation("    Nueva:    {ClaveNueva}", claveNueva);

            return claveNueva;
        }


        // ============================================================
        // AGREGAR ESTOS MÉTODOS AL FINAL DE SriIntegracionService.cs
        // ANTES de la última llave de cierre de la clase
        // ============================================================

        /// <summary>
<<<<<<< HEAD
=======
        /// Genera el XML completo de una factura (version 2.1.0, UTF-8)
        /// </summary>
        // ============================================================
        // REEMPLAZAR ESTE FRAGMENTO EN SriIntegracionService.cs
        // Busca el método GenerarXmlFactura() y reemplázalo completo
        // ============================================================

        /// <summary>
        /// Genera el XML completo de una factura (version 2.1.0, UTF-8)
        /// ✅ CORREGIDO: Usa posiciones correctas para extraer de la clave
        /// </summary>

        // REEMPLAZAR el método GenerarXmlFactura completo (línea ~850 aprox)
        // ============================================================
        // 🔥 CORRECCIÓN: GenerarXmlFactura en SriIntegracionService.cs
        // Ubicación: Línea ~850 aproximadamente
        // ============================================================

        /// <summary>
        /// Genera el XML completo de una factura (version 2.1.0, UTF-8)
        /// ✅ CORREGIDO: Usa la clave de acceso directamente de la factura
        /// </summary>
        private async Task<string> GenerarXmlFactura(Domain.Entities.Factura factura)
        {
            // Cargar configuración de la empresa
            var config = await _context.ConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (config == null)
            {
                throw new InvalidOperationException("No se encontró configuración de la empresa emisora");
            }

            // ✅ VALIDAR QUE LA FACTURA TENGA CLAVE DE ACCESO
            if (string.IsNullOrWhiteSpace(factura.ClaveAcceso))
            {
                throw new InvalidOperationException(
                    "La factura no tiene clave de acceso. Debe generarse antes de crear el XML.");
            }

            if (factura.ClaveAcceso.Length != 49)
            {
                throw new InvalidOperationException(
                    $"La clave de acceso es inválida (longitud {factura.ClaveAcceso.Length}, esperado 49)");
            }

            _logger.LogInformation("  → Generando XML con clave: {Clave}", factura.ClaveAcceso);

            // ✅ Extraer partes del número de factura
            var partes = factura.NumeroFactura.Split('-');
            if (partes.Length != 3)
            {
                throw new InvalidOperationException($"Formato de número de factura inválido: {factura.NumeroFactura}");
            }

            var establecimiento = partes[0].Trim().PadLeft(3, '0');
            var puntoEmision = partes[1].Trim().PadLeft(3, '0');
            var secuencial = partes[2].Trim().PadLeft(9, '0');

            // ✅ VERIFICAR COHERENCIA: Los datos del XML deben coincidir con la clave
            var claveInfo = _claveAccesoGenerator.ExtraerInformacion(factura.ClaveAcceso);

            if (claveInfo.Establecimiento != establecimiento)
            {
                _logger.LogWarning("  ⚠️ Establecimiento en clave ({ClaveEst}) != número factura ({NumEst})",
                    claveInfo.Establecimiento, establecimiento);
            }

            if (claveInfo.PuntoEmision != puntoEmision)
            {
                _logger.LogWarning("  ⚠️ Punto emisión en clave ({ClavePto}) != número factura ({NumPto})",
                    claveInfo.PuntoEmision, puntoEmision);
            }

            if (claveInfo.Secuencial != secuencial)
            {
                _logger.LogWarning("  ⚠️ Secuencial en clave ({ClaveSec}) != número factura ({NumSec})",
                    claveInfo.Secuencial, secuencial);
            }

            // Determinar código de tipo de identificación del comprador
            var tipoIdentificacionComprador = factura.Cliente?.TipoIdentificacion?.CodigoSRI ?? "07";

            // Determinar razón social del comprador
            string razonSocialComprador = factura.Cliente?.Identificacion == "9999999999999"
                ? "CONSUMIDOR FINAL"
                : factura.Cliente?.NombreCompleto()?.Trim() ?? "CONSUMIDOR FINAL";

            var totalSinImpuestos = factura.Subtotal0 + factura.Subtotal15;
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            // ✅ Generar solo los impuestos que realmente aplican
            var xmlImpuestos = new System.Text.StringBuilder();

            if (factura.Subtotal15 > 0)
            {
                xmlImpuestos.AppendLine("            <totalImpuesto>");
                xmlImpuestos.AppendLine("                <codigo>2</codigo>");
                xmlImpuestos.AppendLine("                <codigoPorcentaje>4</codigoPorcentaje>");
                xmlImpuestos.AppendLine($"                <baseImponible>{factura.Subtotal15.ToString("F2", culture)}</baseImponible>");
                xmlImpuestos.AppendLine($"                <valor>{factura.IVA15.ToString("F2", culture)}</valor>");
                xmlImpuestos.AppendLine("            </totalImpuesto>");
            }

            if (factura.Subtotal0 > 0)
            {
                xmlImpuestos.AppendLine("            <totalImpuesto>");
                xmlImpuestos.AppendLine("                <codigo>2</codigo>");
                xmlImpuestos.AppendLine("                <codigoPorcentaje>0</codigoPorcentaje>");
                xmlImpuestos.AppendLine($"                <baseImponible>{factura.Subtotal0.ToString("F2", culture)}</baseImponible>");
                xmlImpuestos.AppendLine("                <valor>0.00</valor>");
                xmlImpuestos.AppendLine("            </totalImpuesto>");
            }

            // ✅✅✅ CRÍTICO: Usar la clave de acceso de la factura
            _logger.LogInformation("  → Insertando clave en XML: {Clave}", factura.ClaveAcceso);

            // ✅✅✅ CRÍTICO: Declaración XML con UTF-8 en MAYÚSCULAS
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""2.1.0"">
    <infoTributaria>
        <ambiente>{config.AmbienteSRI}</ambiente>
        <tipoEmision>{config.TipoEmision ?? "1"}</tipoEmision>
        <razonSocial>{System.Security.SecurityElement.Escape(config.RazonSocial)}</razonSocial>
        <nombreComercial>{System.Security.SecurityElement.Escape(config.NombreComercial ?? config.RazonSocial)}</nombreComercial>
        <ruc>{config.RUC}</ruc>
        <claveAcceso>{factura.ClaveAcceso}</claveAcceso>
        <codDoc>01</codDoc>
        <estab>{establecimiento}</estab>
        <ptoEmi>{puntoEmision}</ptoEmi>
        <secuencial>{secuencial}</secuencial>
        <dirMatriz>{System.Security.SecurityElement.Escape(config.DirMatriz)}</dirMatriz>
    </infoTributaria>
    <infoFactura>
        <fechaEmision>{factura.FechaEmision:dd/MM/yyyy}</fechaEmision>
        <dirEstablecimiento>{System.Security.SecurityElement.Escape(config.DirEstablecimiento ?? config.DirMatriz)}</dirEstablecimiento>
        <tipoIdentificacionComprador>{tipoIdentificacionComprador}</tipoIdentificacionComprador>
        <razonSocialComprador>{System.Security.SecurityElement.Escape(razonSocialComprador)}</razonSocialComprador>
        <identificacionComprador>{factura.Cliente?.Identificacion ?? "9999999999999"}</identificacionComprador>
        <totalSinImpuestos>{totalSinImpuestos.ToString("F2", culture)}</totalSinImpuestos>
        <totalDescuento>{factura.Descuento.ToString("F2", culture)}</totalDescuento>
        <totalConImpuestos>
{xmlImpuestos.ToString().TrimEnd()}
        </totalConImpuestos>
        <propina>{factura.Propina.ToString("F2", culture)}</propina>
        <importeTotal>{factura.ImporteTotal.ToString("F2", culture)}</importeTotal>
        <moneda>DOLAR</moneda>
    </infoFactura>
    <detalles>
{GenerarDetallesXml(factura)}
    </detalles>
</factura>";
        }
        /// <summary>
>>>>>>> 0402e9fc6a8b37751de16810ddff049c1cb863b6
        /// Genera el XML de los detalles de la factura
        /// </summary>
        /// 

        /// <summary>
        /// Genera XML sin firmar y lo firma en un solo paso
        /// </summary>
        // ============================================================
        // REEMPLAZAR MÉTODO COMPLETO EN: SriIntegracionService.cs
        // Ubicación aproximada: Línea ~950
        // ============================================================
        // ============================================================
        // REEMPLAZAR MÉTODO COMPLETO EN: SriIntegracionService.cs
        // Ubicación aproximada: Línea ~950
        // ============================================================

        /// <summary>
        /// Genera XML sin firmar y lo firma en un solo paso
        /// ✅ CORRECCIÓN CRÍTICA: Asegura declaración XML con UTF-8 mayúsculas
        /// 🔥 ESTE MÉTODO DEBE REEMPLAZARSE EN: SriIntegracionService.cs (línea ~950)
        /// </summary>
        private async Task<string> GenerarYFirmarXmlAsync(
            Domain.Entities.Factura factura,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("  → Generando XML con declaración UTF-8...");

            // ═══════════════════════════════════════════════════════
            // PASO 1: GENERAR XML BASE (CON declaración UTF-8)
            // ═══════════════════════════════════════════════════════
            var xmlSinFirmar = await GenerarXmlFactura(factura);

            _logger.LogInformation("  ✓ XML generado correctamente");
            _logger.LogInformation("  → Tamaño: {Size} bytes", xmlSinFirmar.Length);

            // ═══════════════════════════════════════════════════════
            // PASO 2: FIRMAR XML
            // ═══════════════════════════════════════════════════════
            _logger.LogInformation("  → Firmando XML con certificado digital...");

            var xmlFirmado = await _firmaService.FirmarXml(xmlSinFirmar);

            _logger.LogInformation("  ✓ XML firmado exitosamente");
            _logger.LogInformation("  → Tamaño XML firmado: {Size} bytes", xmlFirmado.Length);

            // ═══════════════════════════════════════════════════════
            // ✅✅✅ PASO 3: CORRECCIÓN POST-FIRMA (CRÍTICO)
            // El servicio de firma puede cambiar UTF-8 → utf-8
            // o remover la declaración XML completamente
            // ═══════════════════════════════════════════════════════
            _logger.LogInformation("  → Verificando y corrigiendo encoding...");

            bool necesitaCorreccion = false;

            // ─────────────────────────────────────────────────────
            // Verificación 1: ¿Tiene declaración XML?
            // ─────────────────────────────────────────────────────
            if (!xmlFirmado.TrimStart().StartsWith("<?xml"))
            {
                _logger.LogError("  ❌ XML firmado NO tiene declaración XML");
                _logger.LogInformation("  → Agregando declaración XML...");

                // Agregar declaración al inicio
                xmlFirmado = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{xmlFirmado}";
                necesitaCorreccion = true;

                _logger.LogInformation("  ✓ Declaración XML agregada");
            }

            // ─────────────────────────────────────────────────────
            // Verificación 2: ¿El encoding está en mayúsculas?
            // ─────────────────────────────────────────────────────
            if (xmlFirmado.Contains("encoding=\"utf-8\""))
            {
                _logger.LogWarning("  ⚠️ Encoding en minúsculas detectado");
                _logger.LogInformation("  → Corrigiendo a UTF-8 (mayúsculas)...");

                // Reemplazar encoding minúsculas por mayúsculas
                xmlFirmado = xmlFirmado.Replace("encoding=\"utf-8\"", "encoding=\"UTF-8\"");
                necesitaCorreccion = true;

                _logger.LogInformation("  ✓ Encoding corregido a UTF-8 (mayúsculas)");
            }

            // ─────────────────────────────────────────────────────
            // Verificación 3: Verificación final de la declaración
            // ─────────────────────────────────────────────────────
            var primerosCaracteres = xmlFirmado.Substring(0, Math.Min(60, xmlFirmado.Length));
            _logger.LogInformation("  → Primeros 60 caracteres: {Inicio}", primerosCaracteres);

            if (primerosCaracteres.Contains("encoding=\"UTF-8\""))
            {
                _logger.LogInformation("  ✅ ENCODING CORRECTO: UTF-8 (mayúsculas)");
            }
            else
            {
                _logger.LogError("  ❌ ENCODING INCORRECTO - EL SRI LO RECHAZARÁ");
                _logger.LogError("  → Primeros caracteres: {Chars}", primerosCaracteres);

                throw new InvalidOperationException(
                    "El XML firmado no tiene encoding UTF-8 correcto. " +
                    "Primeros caracteres: " + primerosCaracteres);
            }

            // ─────────────────────────────────────────────────────
            // Verificación 4: ¿Tiene firma digital válida?
            // ─────────────────────────────────────────────────────
            if (!xmlFirmado.Contains("<Signature") && !xmlFirmado.Contains("ds:Signature"))
            {
                _logger.LogError("  ❌ XML NO CONTIENE firma electrónica");
                throw new InvalidOperationException("El XML firmado no contiene firma electrónica válida");
            }

            if (!xmlFirmado.Contains("SignatureValue"))
            {
                _logger.LogError("  ❌ XML NO CONTIENE SignatureValue");
                throw new InvalidOperationException("El XML firmado no contiene SignatureValue");
            }

            _logger.LogInformation("  ✓ Firma digital verificada en el XML");

            // ─────────────────────────────────────────────────────
            // Verificación 5: Log final de confirmación
            // ─────────────────────────────────────────────────────
            if (necesitaCorreccion)
            {
                _logger.LogInformation("  ✓ XML corregido exitosamente");
            }

            _logger.LogInformation("  ✅ XML LISTO PARA ENVIAR AL SRI");
            _logger.LogInformation("  → Tamaño final: {Size} bytes", xmlFirmado.Length);

            return xmlFirmado;
        }


        private string GenerarDetallesXml(Domain.Entities.Factura factura)
        {
            var detallesXml = new System.Text.StringBuilder();
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            foreach (var detalle in factura.Detalles)
            {
                // Determinar el código de porcentaje IVA correcto
                var codigoPorcentaje = detalle.Tarifa > 0 ? "4" : "0"; // 4 = 15% IVA, 0 = 0% IVA

                detallesXml.Append($@"        <detalle>
            <codigoPrincipal>{System.Security.SecurityElement.Escape(detalle.CodigoPrincipal)}</codigoPrincipal>
            <descripcion>{System.Security.SecurityElement.Escape(detalle.Descripcion)}</descripcion>
            <cantidad>{detalle.Cantidad.ToString(culture)}</cantidad>
            <precioUnitario>{detalle.PrecioUnitario.ToString("F6", culture)}</precioUnitario>
            <descuento>{detalle.Descuento.ToString("F2", culture)}</descuento>
            <precioTotalSinImpuesto>{detalle.PrecioTotalSinImpuesto.ToString("F2", culture)}</precioTotalSinImpuesto>
            <impuestos>
                <impuesto>
                    <codigo>2</codigo>
                    <codigoPorcentaje>{codigoPorcentaje}</codigoPorcentaje>
                    <tarifa>{detalle.Tarifa.ToString(culture)}</tarifa>
                    <baseImponible>{detalle.BaseImponible.ToString("F2", culture)}</baseImponible>
                    <valor>{detalle.Valor.ToString("F2", culture)}</valor>
                </impuesto>
            </impuestos>
        </detalle>
");
            }

            return detallesXml.ToString();
        }

        // ============================================================
        // IMPORTANTE: ACTUALIZAR LA LLAMADA EN ProcesarFacturaCompletaAsync
        // ============================================================
        // En la ETAPA 2 (línea ~96 aproximadamente), cambiar:
        //   xmlSinFirmar = GenerarXmlFactura(factura);
        // Por:
        //   xmlSinFirmar = await GenerarXmlFactura(factura);
    }
}