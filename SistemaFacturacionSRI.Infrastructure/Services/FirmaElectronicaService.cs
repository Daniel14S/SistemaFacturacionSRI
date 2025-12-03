// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronicaService.cs
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio de firma electrónica XADES-BES
    /// T-055: Interfaz creada
    /// T-057: Carga de certificado implementada
    /// T-058 a T-063: Implementación de firma
    /// </summary>
    public class FirmaElectronicaService : IFirmaElectronicaService
    {
        private readonly ICertificadoDigitalService _certificadoService;
        private readonly ILogger<FirmaElectronicaService> _logger;
        private X509Certificate2? _certificadoCargado;
        private readonly object _lockCertificado = new object();

        public FirmaElectronicaService(
            ICertificadoDigitalService certificadoService,
            ILogger<FirmaElectronicaService> logger)
        {
            _certificadoService = certificadoService ?? throw new ArgumentNullException(nameof(certificadoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // T-057: Inicializar certificado al crear el servicio
            InicializarCertificado();
        }

        // ============================================================
        // T-057: CARGA Y GESTIÓN DE CERTIFICADO
        // ============================================================

        /// <summary>
        /// T-057: Inicializa y valida el certificado al arrancar el servicio
        /// </summary>
        private void InicializarCertificado()
        {
            try
            {
                _logger.LogInformation("Inicializando certificado digital para firma electrónica...");

                // Cargar certificado
                _certificadoCargado = _certificadoService.CargarCertificado();

                // Validar que sea apto para firma
                var esValido = _certificadoService.ValidarYRegistrarCertificado(_certificadoCargado);

                if (!esValido)
                {
                    _logger.LogError("El certificado cargado NO es válido para firma electrónica");
                    throw new InvalidOperationException(
                        "El certificado digital no es válido para firma electrónica. " +
                        "Revise los errores en el log y corrija la configuración.");
                }

                // Verificar que tenga clave privada (crítico para firma)
                if (!_certificadoCargado.HasPrivateKey)
                {
                    _logger.LogError("El certificado NO tiene clave privada");
                    throw new InvalidOperationException(
                        "El certificado no contiene clave privada. " +
                        "Se requiere un certificado con clave privada para firmar documentos.");
                }

                // Log de éxito
                var info = _certificadoService.ObtenerInformacionCertificado(_certificadoCargado);
                _logger.LogInformation("✅ Certificado inicializado correctamente");
                _logger.LogInformation("   Subject: {Subject}", info.Subject);
                _logger.LogInformation("   Válido hasta: {ValidoHasta:dd/MM/yyyy}", info.ValidoHasta);
                _logger.LogInformation("   Días restantes: {Dias}", info.DiasRestantes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico al inicializar certificado digital");
                _certificadoCargado = null;
                throw new InvalidOperationException(
                    "No se pudo inicializar el servicio de firma electrónica. " +
                    "Verifique la configuración del certificado digital.", ex);
            }
        }

        /// <summary>
        /// T-057: Obtiene el certificado, asegurándose de que esté cargado
        /// </summary>
        private X509Certificate2 ObtenerCertificado()
        {
            lock (_lockCertificado)
            {
                if (_certificadoCargado == null)
                {
                    _logger.LogWarning("Certificado no estaba cargado, recargando...");
                    InicializarCertificado();
                }

                if (_certificadoCargado == null)
                {
                    throw new InvalidOperationException(
                        "No hay certificado digital disponible para firmar. " +
                        "Verifique la configuración.");
                }

                return _certificadoCargado;
            }
        }

        /// <summary>
        /// T-057: Valida el certificado antes de usarlo para firmar
        /// </summary>
        private void ValidarCertificadoParaFirma(X509Certificate2 certificado)
        {
            _logger.LogDebug("Validando certificado antes de firmar...");

            // Verificar que no esté expirado
            if (DateTime.Now > certificado.NotAfter)
            {
                throw new InvalidOperationException(
                    $"El certificado está EXPIRADO desde {certificado.NotAfter:dd/MM/yyyy}. " +
                    "No se puede usar para firmar documentos.");
            }

            // Verificar que ya sea válido
            if (DateTime.Now < certificado.NotBefore)
            {
                throw new InvalidOperationException(
                    $"El certificado aún no es válido. Será válido desde {certificado.NotBefore:dd/MM/yyyy}.");
            }

            // Verificar clave privada
            if (!certificado.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    "El certificado no tiene clave privada. No se puede firmar.");
            }

            // Advertir si está por expirar
            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;
            if (diasRestantes <= 7)
            {
                _logger.LogWarning("⚠️ URGENTE: El certificado expira en {Dias} días!", diasRestantes);
            }

            _logger.LogDebug("✅ Certificado validado correctamente para firma");
        }

        // ============================================================
        // MÉTODOS DE FIRMA (T-058 a T-063: Pendientes)
        // ============================================================

        /// <summary>
        /// T-058 a T-063: Firma un XML con XADES-BES
        /// </summary>
        public async Task<string> FirmarXml(string xmlSinFirmar)
        {
            _logger.LogInformation("Iniciando firma de XML");

            // T-057: Obtener y validar certificado
            var certificado = ObtenerCertificado();
            ValidarCertificadoParaFirma(certificado);

            // Delegar a sobrecarga con certificado
            return await FirmarXml(xmlSinFirmar, certificado);
        }

        /// <summary>
        /// T-058 a T-063: Firma un XML con certificado específico
        /// </summary>
        public async Task<string> FirmarXml(string xmlSinFirmar, X509Certificate2 certificado)
        {
            _logger.LogInformation("Firmando XML con certificado: {Subject}", certificado.Subject);

            // T-057: Validar certificado antes de firmar
            ValidarCertificadoParaFirma(certificado);

            try
            {
                // TODO T-058: Crear estructura SignedInfo
                // TODO T-059: Calcular hash del documento
                // TODO T-060: Firmar con RSA
                // TODO T-061: Incluir certificado en KeyInfo
                // TODO T-062: Agregar SignedProperties
                // TODO T-063: Insertar firma en XML

                await Task.CompletedTask; // Placeholder

                throw new NotImplementedException(
                    "La firma XADES-BES se implementará en las tareas T-058 a T-063. " +
                    "Certificado validado correctamente, listo para firmar.");
            }
            catch (NotImplementedException)
            {
                throw; // Re-lanzar NotImplementedException
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar XML");
                throw new InvalidOperationException(
                    $"Error al firmar el documento: {ex.Message}", ex);
            }
        }

        // ============================================================
        // MÉTODOS DE VALIDACIÓN (Pendientes)
        // ============================================================

        /// <summary>
        /// Valida una firma XADES-BES
        /// </summary>
        public async Task<bool> ValidarFirma(string xmlFirmado)
        {
            var resultado = await ValidarFirmaDetallada(xmlFirmado);
            return resultado.EsValida;
        }

        /// <summary>
        /// Validación detallada de firma
        /// </summary>
        public async Task<ResultadoValidacionFirma> ValidarFirmaDetallada(string xmlFirmado)
        {
            _logger.LogInformation("Validando firma de XML");

            try
            {
                // TODO: Implementar validación completa
                // 1. Verificar que existe nodo Signature
                // 2. Extraer certificado de KeyInfo
                // 3. Validar certificado
                // 4. Recalcular hash del documento
                // 5. Verificar firma RSA
                // 6. Validar SignedProperties

                await Task.CompletedTask; // Placeholder

                return ResultadoValidacionFirma.ConError(
                    "Validación de firma no implementada aún (pendiente en tareas posteriores)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando firma");
                return ResultadoValidacionFirma.ConError($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene información del certificado de un XML firmado
        /// </summary>
        public async Task<InformacionCertificadoFirma> ObtenerInformacionCertificadoFirma(string xmlFirmado)
        {
            _logger.LogInformation("Extrayendo información de certificado del XML firmado");

            await Task.CompletedTask; // Placeholder

            // TODO: Implementar extracción de certificado desde KeyInfo
            throw new NotImplementedException("Pendiente de implementación");
        }

        // ============================================================
        // MÉTODOS AUXILIARES (Implementados en T-055)
        // ============================================================

        /// <summary>
        /// Verifica si un XML tiene firma
        /// </summary>
        public bool TieneFirma(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                // Buscar nodo Signature en namespace de firma digital
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                var signatureNode = doc.SelectSingleNode("//ds:Signature", nsmgr);
                
                var tieneFirma = signatureNode != null;
                _logger.LogDebug("XML tiene firma: {TieneFirma}", tieneFirma);

                return tieneFirma;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando si XML tiene firma");
                return false;
            }
        }

        /// <summary>
        /// Extrae el XML original de un XML firmado
        /// </summary>
        public string ExtraerXmlOriginal(string xmlFirmado)
        {
            _logger.LogInformation("Extrayendo XML original");

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlFirmado);

                // Buscar y eliminar nodo Signature
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                var signatureNode = doc.SelectSingleNode("//ds:Signature", nsmgr);
                
                if (signatureNode != null && signatureNode.ParentNode != null)
                {
                    signatureNode.ParentNode.RemoveChild(signatureNode);
                    _logger.LogInformation("Nodo de firma eliminado correctamente");
                }

                return doc.OuterXml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo XML original");
                throw new InvalidOperationException("Error al extraer XML original", ex);
            }
        }

        /// <summary>
        /// T-057: Obtiene el certificado configurado
        /// </summary>
        public X509Certificate2 ObtenerCertificadoConfiguracion()
        {
            return ObtenerCertificado();
        }
    }
}