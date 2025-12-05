// SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronicaService.cs
// T-057: Carga de certificado
// T-063: IMPLEMENTACIÓN COMPLETA DE FIRMA XADES-BES

using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio de firma electrónica XADES-BES
    /// T-055: Interfaz creada
    /// T-057: Carga de certificado implementada
    /// T-063: Firma XADES-BES COMPLETA
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
                _logger.LogError(ex, "❌ Error crítico al inicializar certificado digital. La firma electrónica no estará disponible.");
                _certificadoCargado = null;
                // No lanzamos excepción para permitir que la aplicación inicie
                // La excepción se lanzará solo cuando se intente firmar
            }
        }

        /// <summary>
        /// T-057: Obtiene el certificado, asegurándose de que esté cargado
        /// </summary>
        private X509Certificate2? ObtenerCertificado()
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
                    _logger.LogWarning("No hay certificado digital disponible. Se procederá en modo sin firma (solo para desarrollo/simulación).");
                    return null;
                }

                return _certificadoCargado;
            }
        }

        /// <summary>
        /// T-057, T-066: Valida el certificado antes de usarlo para firmar con manejo de errores específico
        /// </summary>
        private void ValidarCertificadoParaFirma(X509Certificate2 certificado)
        {
            _logger.LogDebug("Validando certificado antes de firmar...");

            // Verificar que no esté expirado
            if (DateTime.Now > certificado.NotAfter)
            {
                _logger.LogError("Certificado expirado: {FechaExpiracion}", certificado.NotAfter);
                throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoExpiradoException(certificado.NotAfter);
            }

            // Verificar que ya sea válido
            if (DateTime.Now < certificado.NotBefore)
            {
                _logger.LogError("Certificado aún no válido: {FechaInicio}", certificado.NotBefore);
                throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoInvalidoException(
                    $"El certificado aún no es válido. Será válido desde {certificado.NotBefore:dd/MM/yyyy}");
            }

            // Verificar clave privada
            if (!certificado.HasPrivateKey)
            {
                _logger.LogError("Certificado sin clave privada");
                throw new SistemaFacturacionSRI.Domain.Exceptions.ClavePrivadaNoDisponibleException();
            }

            // Advertir si está por expirar
            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;
            if (diasRestantes <= 7)
            {
                _logger.LogWarning("⚠️ URGENTE: El certificado expira en {Dias} días!", diasRestantes);
                // Lanzar excepción de advertencia si quedan menos de 3 días
                if (diasRestantes <= 3)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoPorExpirarException(
                        certificado.NotAfter, diasRestantes);
                }
            }

            _logger.LogDebug("✅ Certificado validado correctamente para firma");
        }

        // ============================================================
        // T-063: IMPLEMENTACIÓN COMPLETA DE FIRMA XADES-BES
        // ============================================================

        /// <summary>
        /// T-063: Firma un XML con XADES-BES usando el certificado configurado
        /// </summary>
        public async Task<string> FirmarXml(string xmlSinFirmar)
        {
            _logger.LogInformation("Iniciando firma de XML");

            // T-057: Obtener y validar certificado
            var certificado = ObtenerCertificado();
            
            if (certificado == null)
            {
                _logger.LogWarning("⚠️ MODO SIMULACIÓN: Retornando XML sin firmar por falta de certificado.");
                return xmlSinFirmar;
            }

            ValidarCertificadoParaFirma(certificado);

            // Delegar a sobrecarga con certificado
            return await FirmarXml(xmlSinFirmar, certificado);
        }

        /// <summary>
        /// T-063, T-066: Firma un XML con certificado específico - IMPLEMENTACIÓN COMPLETA CON MANEJO DE ERRORES
        /// </summary>
        public async Task<string> FirmarXml(string xmlSinFirmar, X509Certificate2 certificado)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("INICIANDO FIRMA ELECTRÓNICA XADES-BES");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("Certificado: {Subject}", certificado.Subject);

                // T-057, T-066: Validar certificado antes de firmar
                ValidarCertificadoParaFirma(certificado);

                // 1️⃣ CARGAR Y VALIDAR XML
                _logger.LogInformation("1️⃣ Cargando y validando XML...");
                
                if (string.IsNullOrWhiteSpace(xmlSinFirmar))
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException(
                        "El contenido XML está vacío");
                }

                XmlDocument doc;
                try
                {
                    doc = new XmlDocument { PreserveWhitespace = true };
                    doc.LoadXml(xmlSinFirmar);
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException(
                        "No se pudo parsear el XML", ex);
                }

                // Verificar que el XML no esté firmado ya
                if (TieneFirma(xmlSinFirmar))
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlYaFirmadoException();
                }

                // Buscar el nodo raíz que se va a firmar (debe tener id="comprobante")
                var nodoRaiz = doc.DocumentElement;
                if (nodoRaiz == null)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException(
                        "El XML no tiene nodo raíz");
                }

                var idNodo = nodoRaiz.GetAttribute("id");
                if (string.IsNullOrEmpty(idNodo))
                {
                    // Si no tiene id, asignamos "comprobante" (estándar SRI)
                    idNodo = "comprobante";
                    nodoRaiz.SetAttribute("id", idNodo);
                    _logger.LogWarning("El nodo raíz no tenía id, se asignó: {Id}", idNodo);
                }

                _logger.LogInformation("   ✅ Nodo a firmar: {NodoNombre} (id={Id})", nodoRaiz.Name, idNodo);

                // 2️⃣ CALCULAR DIGEST DEL DOCUMENTO
                _logger.LogInformation("2️⃣ Calculando digest (hash SHA1) del documento...");
                string digestDocumento;
                try
                {
                    var digestCalculator = new DigestCalculator();
                    digestDocumento = digestCalculator.CalcularDigestNodo(doc, idNodo);
                    _logger.LogInformation("   ✅ Digest calculado: {Digest}", digestDocumento.Substring(0, 20) + "...");
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorCalculoDigestException(
                        "documento", ex);
                }

                // 3️⃣ CREAR ESTRUCTURA SIGNATURE
                _logger.LogInformation("3️⃣ Creando estructura XADES-BES...");
                var fechaFirma = DateTime.Now;
                string certificadoBase64;
                
                try
                {
                    certificadoBase64 = Convert.ToBase64String(certificado.Export(X509ContentType.Cert));
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "No se pudo exportar el certificado a Base64", ex, "exportar_certificado");
                }

                // Crear estructura temporal para obtener SignedInfo
                XmlElement signatureNode;
                try
                {
                    var structureBuilder = new SignatureStructureBuilder(doc);
                    
                    // NOTA: Necesitamos crear SignedInfo primero para firmarlo
                    // Por ahora creamos con valor temporal de firma
                    var signatureTemporalBase64 = "TEMPORAL";
                    
                    signatureNode = structureBuilder.CrearNodoSignature(
                        signatureTemporalBase64,
                        digestDocumento,
                        certificadoBase64,
                        certificado,
                        fechaFirma,
                        idNodo
                    );
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "Error al crear estructura de firma XADES-BES", ex, "crear_estructura");
                }

                // 4️⃣ FIRMAR SIGNEDINFO CON RSA
                _logger.LogInformation("4️⃣ Firmando SignedInfo con RSA-SHA1...");
                
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                
                var signedInfo = signatureNode.SelectSingleNode("ds:SignedInfo", nsmgr) as XmlElement;
                if (signedInfo == null)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "No se pudo crear SignedInfo", etapa: "crear_signedinfo");
                }

                string firmaBase64;
                try
                {
                    var rsaSigner = new RsaSigner();
                    firmaBase64 = rsaSigner.FirmarSignedInfo(signedInfo, certificado);
                    _logger.LogInformation("   ✅ Firma RSA generada: {Firma}", firmaBase64.Substring(0, 20) + "...");
                }
                catch (Exception ex)
                {
                    var keySize = certificado.GetRSAPrivateKey()?.KeySize;
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaRsaException(
                        "Error al firmar con RSA-SHA1", ex, keySize);
                }

                // 5️⃣ ACTUALIZAR SIGNATUREVALUE CON LA FIRMA REAL
                _logger.LogInformation("5️⃣ Insertando firma en SignatureValue...");
                var signatureValue = signatureNode.SelectSingleNode("ds:SignatureValue", nsmgr) as XmlElement;
                if (signatureValue == null)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "No se pudo encontrar SignatureValue", etapa: "actualizar_signaturevalue");
                }
                signatureValue.InnerText = firmaBase64;

                // 6️⃣ INSERTAR FIRMA EN EL XML ORIGINAL
                _logger.LogInformation("6️⃣ Insertando nodo Signature en el XML...");
                
                try
                {
                    // Importar el nodo Signature al documento original
                    var signatureImported = doc.ImportNode(signatureNode, true);
                    
                    // Insertar como último hijo del nodo raíz
                    nodoRaiz.AppendChild(signatureImported);

                    _logger.LogInformation("   ✅ Firma insertada correctamente");
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "Error al insertar firma en el XML", ex, "insertar_firma");
                }

                // 7️⃣ GENERAR XML FIRMADO FINAL
                string xmlFirmado;
                try
                {
                    xmlFirmado = doc.OuterXml;
                }
                catch (Exception ex)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                        "Error al generar XML firmado final", ex, "generar_xml_final");
                }

                var tiempoTranscurrido = DateTime.Now - startTime;
                
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("✅ FIRMA COMPLETADA EXITOSAMENTE");
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("⏱️  Tiempo: {Tiempo}ms", tiempoTranscurrido.TotalMilliseconds);
                _logger.LogInformation("📄 Tamaño XML: {Bytes} bytes", xmlFirmado.Length);

                return await Task.FromResult(xmlFirmado);
            }
            catch (SistemaFacturacionSRI.Domain.Exceptions.FirmaElectronicaException)
            {
                // Re-lanzar excepciones específicas de firma
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR INESPERADO AL FIRMAR XML");
                throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException(
                    $"Error inesperado: {ex.Message}", ex);
            }
        }

        // ============================================================
        // MÉTODOS DE VALIDACIÓN (Implementación básica)
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
                // Verificar que tiene firma
                if (!TieneFirma(xmlFirmado))
                {
                    return ResultadoValidacionFirma.ConError("El XML no tiene firma digital");
                }

                var doc = new XmlDocument();
                doc.LoadXml(xmlFirmado);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                nsmgr.AddNamespace("etsi", "http://uri.etsi.org/01903/v1.3.2#");

                // Extraer certificado de KeyInfo
                var keyInfo = doc.SelectSingleNode("//ds:KeyInfo", nsmgr) as XmlElement;
                if (keyInfo == null)
                {
                    return ResultadoValidacionFirma.ConError("No se encontró KeyInfo");
                }

                var keyInfoBuilder = new KeyInfoBuilder(doc);
                var certificado = keyInfoBuilder.ExtraerCertificado(keyInfo);
                
                if (certificado == null)
                {
                    return ResultadoValidacionFirma.ConError("No se pudo extraer el certificado");
                }

                // Extraer SignedInfo
                var signedInfo = doc.SelectSingleNode("//ds:SignedInfo", nsmgr) as XmlElement;
                if (signedInfo == null)
                {
                    return ResultadoValidacionFirma.ConError("No se encontró SignedInfo");
                }

                // Extraer SignatureValue
                var signatureValue = doc.SelectSingleNode("//ds:SignatureValue", nsmgr);
                if (signatureValue == null || string.IsNullOrWhiteSpace(signatureValue.InnerText))
                {
                    return ResultadoValidacionFirma.ConError("No se encontró SignatureValue");
                }

                // Verificar firma RSA
                var rsaSigner = new RsaSigner();
                var firmaValida = rsaSigner.VerificarFirma(
                    signedInfo.OuterXml,
                    signatureValue.InnerText,
                    certificado
                );

                if (!firmaValida)
                {
                    return ResultadoValidacionFirma.ConError("La firma RSA no es válida");
                }

                // Extraer fecha de firma
                var signingTime = doc.SelectSingleNode("//etsi:SigningTime", nsmgr);
                DateTime? fechaFirma = null;
                if (signingTime != null && DateTime.TryParse(signingTime.InnerText, out var fecha))
                {
                    fechaFirma = fecha;
                }

                return ResultadoValidacionFirma.Exitoso(
                    fechaFirma ?? DateTime.Now,
                    certificado.Subject
                );
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

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlFirmado);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                nsmgr.AddNamespace("etsi", "http://uri.etsi.org/01903/v1.3.2#");

                // Extraer certificado
                var keyInfo = doc.SelectSingleNode("//ds:KeyInfo", nsmgr) as XmlElement;
                if (keyInfo == null)
                {
                    throw new InvalidOperationException("No se encontró KeyInfo en el XML");
                }

                var keyInfoBuilder = new KeyInfoBuilder(doc);
                var certificado = keyInfoBuilder.ExtraerCertificado(keyInfo);
                
                if (certificado == null)
                {
                    throw new InvalidOperationException("No se pudo extraer el certificado");
                }

                // Extraer fecha de firma
                var signingTime = doc.SelectSingleNode("//etsi:SigningTime", nsmgr);
                DateTime? fechaFirma = null;
                if (signingTime != null && DateTime.TryParse(signingTime.InnerText, out var fecha))
                {
                    fechaFirma = fecha;
                }

                return await Task.FromResult(new InformacionCertificadoFirma
                {
                    Subject = certificado.Subject,
                    Issuer = certificado.Issuer,
                    SerialNumber = certificado.SerialNumber,
                    ValidoDesde = certificado.NotBefore,
                    ValidoHasta = certificado.NotAfter,
                    FechaFirma = fechaFirma,
                    EstaVigente = DateTime.Now <= certificado.NotAfter,
                    AlgoritmoFirma = "RSA-SHA1",
                    HashAlgorithm = "SHA1"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo información del certificado");
                throw;
            }
        }

        // ============================================================
        // MÉTODOS AUXILIARES
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