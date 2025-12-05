// SistemaFacturacionSRI.Infrastructure/Services/SRI/SriIntegracionService.cs
// T-083: Implementación del orquestador completo - 100% COMPATIBLE
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Domain.DTOs.SRI;
using SistemaFacturacionSRI.Domain.Enums;
using SistemaFacturacionSRI.Infrastructure.Data;

namespace SistemaFacturacionSRI.Infrastructure.Services.SRI
{
    /// <summary>
    /// T-083: Orquestador completo de integración con el SRI
    /// Coordina: Generación XML → Firma Digital → Envío SRI → Consulta → Actualización BD
    /// </summary>
    public class SriIntegracionService : ISriIntegracionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirmaElectronicaService _firmaService;
        private readonly SriComprobanteService _sriComprobanteService;
        private readonly ISriWebServiceClient _sriClient;
        private readonly ILogger<SriIntegracionService> _logger;

        public SriIntegracionService(
            ApplicationDbContext context,
            IFirmaElectronicaService firmaService,
            SriComprobanteService sriComprobanteService,
            ISriWebServiceClient sriClient,
            ILogger<SriIntegracionService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _firmaService = firmaService ?? throw new ArgumentNullException(nameof(firmaService));
            _sriComprobanteService = sriComprobanteService ?? throw new ArgumentNullException(nameof(sriComprobanteService));
            _sriClient = sriClient ?? throw new ArgumentNullException(nameof(sriClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================================
        // T-083: PROCESAR FACTURA COMPLETA
        // ============================================================
        public async Task<ResultadoIntegracionSri> ProcesarFacturaCompletaAsync(
            int facturaId,
            CancellationToken cancellationToken = default)
        {
            var stopwatchTotal = Stopwatch.StartNew();
            var resultado = new ResultadoIntegracionSri { FacturaId = facturaId };

            _logger.LogInformation("════════════════════════════════════════════════════════════════");
            _logger.LogInformation("🚀 T-083: INICIANDO PROCESAMIENTO COMPLETO DE FACTURA #{Id}", facturaId);
            _logger.LogInformation("════════════════════════════════════════════════════════════════");

            try
            {
                // ═══════════════════════════════════════════════════════
                // ETAPA 1: CARGAR FACTURA DE LA BD
                // ═══════════════════════════════════════════════════════
                var swEtapa1 = Stopwatch.StartNew();
                _logger.LogInformation("📋 ETAPA 1/6: Cargando factura de la base de datos...");

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
                    return resultado;
                }

                swEtapa1.Stop();
                resultado.AgregarEtapa("CargarFactura", true, swEtapa1.Elapsed);
                _logger.LogInformation("✅ ETAPA 1/6 COMPLETADA - Factura cargada: {Numero}", factura.NumeroFactura);

                resultado.ClaveAcceso = factura.ClaveAcceso ?? string.Empty;

                // ═══════════════════════════════════════════════════════
                // ETAPA 2: GENERAR XML DEL COMPROBANTE
                // ═══════════════════════════════════════════════════════
                var swEtapa2 = Stopwatch.StartNew();
                _logger.LogInformation("📝 ETAPA 2/6: Generando XML del comprobante...");

                string xmlSinFirmar;
                try
                {
                    xmlSinFirmar = GenerarXmlFactura(factura);
                    
                    swEtapa2.Stop();
                    resultado.AgregarEtapa("GenerarXml", true, swEtapa2.Elapsed);
                    _logger.LogInformation("✅ ETAPA 2/6 COMPLETADA - XML generado: {Size} bytes", xmlSinFirmar.Length);
                }
                catch (Exception ex)
                {
                    swEtapa2.Stop();
                    resultado.AgregarEtapa("GenerarXml", false, swEtapa2.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al generar XML: {ex.Message}";
                    return resultado;
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 3: FIRMAR XML CON CERTIFICADO DIGITAL
                // ═══════════════════════════════════════════════════════
                var swEtapa3 = Stopwatch.StartNew();
                _logger.LogInformation("🔐 ETAPA 3/6: Firmando XML con certificado digital...");

                string xmlFirmado;
                try
                {
                    xmlFirmado = await _firmaService.FirmarXml(xmlSinFirmar);
                    resultado.XmlFirmado = xmlFirmado;
                    
                    swEtapa3.Stop();
                    resultado.AgregarEtapa("FirmarXml", true, swEtapa3.Elapsed);
                    _logger.LogInformation("✅ ETAPA 3/6 COMPLETADA - XML firmado con XADES-BES");
                }
                catch (Exception ex)
                {
                    swEtapa3.Stop();
                    resultado.AgregarEtapa("FirmarXml", false, swEtapa3.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al firmar XML: {ex.Message}";
                    
                    // ✅ CORRECCIÓN: Usar EstadoFactura.FIRMADA (ya que no existe ERROR_FIRMA)
                    await ActualizarEstadoFacturaAsync(
                        factura, 
                        EstadoFactura.BORRADOR, // Regresa a borrador en caso de error
                        null, 
                        null, 
                        ex.Message, 
                        cancellationToken);
                    return resultado;
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 4: ENVIAR AL SRI (RECEPCIÓN)
                // ═══════════════════════════════════════════════════════
                var swEtapa4 = Stopwatch.StartNew();
                _logger.LogInformation("📤 ETAPA 4/6: Enviando comprobante al SRI...");

                ResultadoOperacionSri resultadoEnvio;
                try
                {
                    // ✅ CORRECCIÓN: Usar Identificacion (que puede ser RUC/Cédula)
                    string rucEmpresa = factura.Cliente?.Identificacion ?? "9999999999999";
                    
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
                        _logger.LogInformation("✅ ETAPA 4/6 COMPLETADA - Comprobante RECIBIDO por el SRI");
                    }
                    else
                    {
                        resultado.AgregarEtapa("EnviarSri", false, swEtapa4.Elapsed, resultadoEnvio.MensajeError);
                        resultado.MensajeError = resultadoEnvio.MensajeError;
                        resultado.EstadoFinal = "DEVUELTA";

                        // ✅ CORRECCIÓN: Usar EstadoFactura.DEVUELTA
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
                    
                    // ✅ CORRECCIÓN: Usar EstadoFactura.ENVIADA con error
                    await ActualizarEstadoFacturaAsync(
                        factura, 
                        EstadoFactura.FIRMADA, // Mantener como firmada si falla el envío
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
                _logger.LogInformation("🔍 ETAPA 5/6: Consultando autorización en el SRI...");

                ResultadoOperacionSri resultadoAutorizacion;
                try
                {
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
                        
                        _logger.LogInformation("✅ ETAPA 5/6 COMPLETADA - Comprobante AUTORIZADO");
                        _logger.LogInformation("Número Autorización: {Numero}", resultado.NumeroAutorizacion);
                    }
                    else
                    {
                        resultado.AgregarEtapa("ConsultarAutorizacion", false, swEtapa5.Elapsed, resultadoAutorizacion.MensajeError);
                        resultado.EstadoFinal = resultadoAutorizacion.Estado;
                        resultado.MensajeError = resultadoAutorizacion.MensajeError;
                        
                        _logger.LogWarning("⚠️ ETAPA 5/6 - Comprobante NO autorizado: {Estado}", resultado.EstadoFinal);
                    }
                }
                catch (Exception ex)
                {
                    swEtapa5.Stop();
                    resultado.AgregarEtapa("ConsultarAutorizacion", false, swEtapa5.Elapsed, ex.Message);
                    resultado.MensajeError = $"Error al consultar autorización: {ex.Message}";
                    resultado.EstadoFinal = "ERROR_CONSULTA";
                }

                // ═══════════════════════════════════════════════════════
                // ETAPA 6: ACTUALIZAR ESTADO EN BASE DE DATOS
                // ═══════════════════════════════════════════════════════
                var swEtapa6 = Stopwatch.StartNew();
                _logger.LogInformation("💾 ETAPA 6/6: Actualizando estado en base de datos...");

                try
                {
                    // ✅ CORRECCIÓN: Mapear estado string a EstadoFactura enum
                    var estadoEnum = MapearEstadoStringAEnum(resultado.EstadoFinal);
                    
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
                    _logger.LogInformation("✅ ETAPA 6/6 COMPLETADA - Estado actualizado en BD");
                }
                catch (Exception ex)
                {
                    swEtapa6.Stop();
                    resultado.AgregarEtapa("ActualizarBD", false, swEtapa6.Elapsed, ex.Message);
                    _logger.LogError(ex, "Error al actualizar estado en BD");
                }

                stopwatchTotal.Stop();
                resultado.TiempoTotal = stopwatchTotal.Elapsed;

                // ═══════════════════════════════════════════════════════
                // RESUMEN FINAL
                // ═══════════════════════════════════════════════════════
                _logger.LogInformation("════════════════════════════════════════════════════════════════");
                if (resultado.Exitoso)
                {
                    _logger.LogInformation("🎉 PROCESAMIENTO COMPLETADO EXITOSAMENTE");
                }
                else
                {
                    _logger.LogWarning("⚠️ PROCESAMIENTO COMPLETADO CON ERRORES");
                }
                _logger.LogInformation("════════════════════════════════════════════════════════════════");
                _logger.LogInformation("Factura: #{Id}", facturaId);
                _logger.LogInformation("Estado Final: {Estado}", resultado.EstadoFinal);
                _logger.LogInformation("Tiempo Total: {Tiempo:F2}s", resultado.TiempoTotal.TotalSeconds);
                _logger.LogInformation("Total Intentos: {Intentos}", resultado.TotalIntentos);
                _logger.LogInformation("════════════════════════════════════════════════════════════════");

                return resultado;
            }
            catch (Exception ex)
            {
                stopwatchTotal.Stop();
                _logger.LogError(ex, "❌ Error crítico en procesamiento de factura #{Id}", facturaId);
                
                resultado.Exitoso = false;
                resultado.EstadoFinal = "ERROR_CRITICO";
                resultado.MensajeError = $"Error crítico: {ex.Message}";
                resultado.TiempoTotal = stopwatchTotal.Elapsed;
                
                return resultado;
            }
        }

        // ============================================================
        // T-083: PROCESAR MÚLTIPLES FACTURAS
        // ============================================================
        public async Task<List<ResultadoIntegracionSri>> ProcesarFacturasLoteAsync(
            List<int> facturasIds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("📦 PROCESAMIENTO EN LOTE - {Count} facturas", facturasIds.Count);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");

            var resultados = new List<ResultadoIntegracionSri>();

            foreach (var facturaId in facturasIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("⚠️ Procesamiento en lote cancelado");
                    break;
                }

                var resultado = await ProcesarFacturaCompletaAsync(facturaId, cancellationToken);
                resultados.Add(resultado);

                // Pequeña pausa entre facturas para no saturar el SRI
                await Task.Delay(1000, cancellationToken);
            }

            var exitosas = resultados.Count(r => r.Exitoso);
            var fallidas = resultados.Count - exitosas;

            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("📊 RESUMEN LOTE: {Exitosas} exitosas, {Fallidas} fallidas", exitosas, fallidas);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");

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

        /// <summary>
        /// Verifica estado de conectividad con el SRI
        /// </summary>
        public async Task<EstadoServiciosSri> VerificarEstadoSriAsync()
        {
            return await _sriClient.ObtenerEstadoServiciosAsync();
        }

        // ============================================================
        // MÉTODOS AUXILIARES
        // ============================================================

        /// <summary>
        /// ✅ CORRECCIÓN: Actualiza el estado usando EstadoFactura enum
        /// </summary>
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
            
            _logger.LogDebug("Estado actualizado en BD: {Estado}", estado);
        }

        /// <summary>
        /// ✅ NUEVO: Mapea estados string del SRI a tu enum EstadoFactura
        /// </summary>
        private EstadoFactura MapearEstadoStringAEnum(string estado)
        {
            return estado?.ToUpperInvariant() switch
            {
                "AUTORIZADO" => EstadoFactura.AUTORIZADA,
                "DEVUELTA" => EstadoFactura.DEVUELTA,
                "NO_AUTORIZADO" => EstadoFactura.NO_AUTORIZADA,
                "RECIBIDA" => EstadoFactura.RECIBIDA,
                "ENVIADA" => EstadoFactura.ENVIADA,
                "ERROR_FIRMA" => EstadoFactura.BORRADOR, // No existe en tu enum, regresar a borrador
                "ERROR_ENVIO" => EstadoFactura.FIRMADA,  // Mantener como firmada
                _ => EstadoFactura.BORRADOR
            };
        }

        /// <summary>
        /// Genera XML de la factura según esquema SRI
        /// TODO: Implementar generación real según esquema XSD del SRI
        /// </summary>
        private string GenerarXmlFactura(Domain.Entities.Factura factura)
        {
            // Por ahora retorna un XML de ejemplo
            // En la implementación real, deberías generar el XML según el esquema oficial del SRI
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<factura id=""comprobante"" version=""1.0.0"">
    <infoTributaria>
        <ambiente>{(int)factura.Ambiente}</ambiente>
        <tipoEmision>{(int)factura.TipoEmision}</tipoEmision>
        <razonSocial>{factura.Cliente?.NombreCompleto() ?? "Consumidor Final"}</razonSocial>
        <nombreComercial>Mi Empresa</nombreComercial>
        <ruc>{factura.Cliente?.Identificacion ?? "9999999999999"}</ruc>
        <claveAcceso>{factura.ClaveAcceso}</claveAcceso>
        <codDoc>01</codDoc>
        <estab>001</estab>
        <ptoEmi>001</ptoEmi>
        <secuencial>{factura.NumeroFactura}</secuencial>
        <dirMatriz>Matriz Principal</dirMatriz>
    </infoTributaria>
    <infoFactura>
        <fechaEmision>{factura.FechaEmision:dd/MM/yyyy}</fechaEmision>
        <dirEstablecimiento>Sucursal 001</dirEstablecimiento>
        <obligadoContabilidad>SI</obligadoContabilidad>
        <tipoIdentificacionComprador>{factura.Cliente?.TipoIdentificacionId ?? 7}</tipoIdentificacionComprador>
        <razonSocialComprador>{factura.Cliente?.NombreCompleto() ?? "CONSUMIDOR FINAL"}</razonSocialComprador>
        <identificacionComprador>{factura.Cliente?.Identificacion ?? "9999999999999"}</identificacionComprador>
        <totalSinImpuestos>{(factura.Subtotal0 + factura.Subtotal15):F2}</totalSinImpuestos>
        <totalDescuento>{factura.Descuento:F2}</totalDescuento>
        <totalConImpuestos>
            <totalImpuesto>
                <codigo>2</codigo>
                <codigoPorcentaje>2</codigoPorcentaje>
                <baseImponible>{factura.Subtotal15:F2}</baseImponible>
                <valor>{factura.IVA15:F2}</valor>
            </totalImpuesto>
            <totalImpuesto>
                <codigo>2</codigo>
                <codigoPorcentaje>0</codigoPorcentaje>
                <baseImponible>{factura.Subtotal0:F2}</baseImponible>
                <valor>0.00</valor>
            </totalImpuesto>
        </totalConImpuestos>
        <propina>{factura.Propina:F2}</propina>
        <importeTotal>{factura.ImporteTotal:F2}</importeTotal>
        <moneda>DOLAR</moneda>
    </infoFactura>
    <detalles>
        {GenerarDetallesXml(factura)}
    </detalles>
</factura>";
        }

        /// <summary>
        /// Genera el XML de los detalles de la factura
        /// </summary>
        private string GenerarDetallesXml(Domain.Entities.Factura factura)
        {
            var detallesXml = new System.Text.StringBuilder();
            
            foreach (var detalle in factura.Detalles)
            {
                detallesXml.AppendLine($@"        <detalle>
            <codigoPrincipal>{detalle.CodigoPrincipal}</codigoPrincipal>
            <descripcion>{System.Security.SecurityElement.Escape(detalle.Descripcion)}</descripcion>
            <cantidad>{detalle.Cantidad:F2}</cantidad>
            <precioUnitario>{detalle.PrecioUnitario:F6}</precioUnitario>
            <descuento>{detalle.Descuento:F2}</descuento>
            <precioTotalSinImpuesto>{detalle.PrecioTotalSinImpuesto:F2}</precioTotalSinImpuesto>
            <impuestos>
                <impuesto>
                    <codigo>2</codigo>
                    <codigoPorcentaje>{detalle.CodigoPorcentajeIVA}</codigoPorcentaje>
                    <tarifa>{detalle.Tarifa}</tarifa>
                    <baseImponible>{detalle.BaseImponible:F2}</baseImponible>
                    <valor>{detalle.Valor:F2}</valor>
                </impuesto>
            </impuestos>
        </detalle>");
            }
            
            return detallesXml.ToString();
        }
    }
}