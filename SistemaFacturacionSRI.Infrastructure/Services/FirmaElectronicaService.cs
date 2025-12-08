using FirmaXadesNet;
using FirmaXadesNet.Crypto;
using FirmaXadesNet.Signature;
using FirmaXadesNet.Signature.Parameters;
using Microsoft.Extensions.Logging;
using SistemaFacturacionSRI.Domain.Interfaces.Services;
using SistemaFacturacionSRI.Infrastructure.Services.FirmaElectronica;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio de firma electrónica XADES-BES con logging detallado
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

            InicializarCertificado();
        }

        // ============================================================
        // CARGA Y GESTIÓN DE CERTIFICADO
        // ============================================================

        private void InicializarCertificado()
        {
            try
            {
                _logger.LogInformation("\n╔════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║   INICIALIZANDO CERTIFICADO DIGITAL                  ║");
                _logger.LogInformation("╚════════════════════════════════════════════════════════╝");

                // Cargar certificado
                _logger.LogInformation("[CERT-1] Cargando certificado desde configuración...");
                _certificadoCargado = _certificadoService.CargarCertificado();

                // Validar que sea apto para firma
                _logger.LogInformation("[CERT-2] Validando certificado para firma electrónica...");
                var esValido = _certificadoService.ValidarYRegistrarCertificado(_certificadoCargado);

                if (!esValido)
                {
                    _logger.LogError("  ❌ El certificado NO es válido para firma electrónica");
                    throw new InvalidOperationException(
                        "El certificado digital no es válido para firma electrónica. " +
                        "Revise los errores en el log y corrija la configuración.");
                }

                // Verificar clave privada (crítico)
                if (!_certificadoCargado.HasPrivateKey)
                {
                    _logger.LogError("  ❌ El certificado NO tiene clave privada");
                    throw new InvalidOperationException(
                        "El certificado no contiene clave privada. " +
                        "Se requiere un certificado con clave privada para firmar documentos.");
                }

                var info = _certificadoService.ObtenerInformacionCertificado(_certificadoCargado);
                _logger.LogInformation("\n[CERT-3] ✅ Certificado inicializado correctamente");
                _logger.LogInformation("  → Subject: {Subject}", info.Subject);
                _logger.LogInformation("  → Válido hasta: {ValidoHasta:dd/MM/yyyy}", info.ValidoHasta);
                _logger.LogInformation("  → Días restantes: {Dias}", info.DiasRestantes);
                _logger.LogInformation("  → Tiene clave privada: ✓ Sí");
                _logger.LogInformation("  → Tamaño de clave: {KeySize} bits", _certificadoCargado.GetRSAPrivateKey()?.KeySize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico al inicializar certificado digital");
                _certificadoCargado = null;
            }
        }

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
                    _logger.LogWarning("⚠️ No hay certificado digital disponible.");
                    return null;
                }

                return _certificadoCargado;
            }
        }

        private void ValidarCertificadoParaFirma(X509Certificate2 certificado)
        {
            _logger.LogDebug("[VALIDACIÓN] Verificando certificado antes de firmar...");

            // Verificar que no esté expirado
            if (DateTime.Now > certificado.NotAfter)
            {
                _logger.LogError("  ❌ Certificado expirado: {FechaExpiracion}", certificado.NotAfter);
                throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoExpiradoException(certificado.NotAfter);
            }

            // Verificar que ya sea válido
            if (DateTime.Now < certificado.NotBefore)
            {
                _logger.LogError("  ❌ Certificado aún no válido: {FechaInicio}", certificado.NotBefore);
                throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoInvalidoException(
                    $"El certificado aún no es válido. Será válido desde {certificado.NotBefore:dd/MM/yyyy}");
            }

            // Verificar clave privada
            if (!certificado.HasPrivateKey)
            {
                _logger.LogError("  ❌ Certificado sin clave privada");
                throw new SistemaFacturacionSRI.Domain.Exceptions.ClavePrivadaNoDisponibleException();
            }

            // Advertir si está por expirar
            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;
            if (diasRestantes <= 7)
            {
                _logger.LogWarning("⚠️ URGENTE: El certificado expira en {Dias} días!", diasRestantes);
                if (diasRestantes <= 3)
                {
                    throw new SistemaFacturacionSRI.Domain.Exceptions.CertificadoPorExpirarException(
                        certificado.NotAfter, diasRestantes);
                }
            }

            _logger.LogDebug("  ✓ Certificado validado correctamente");
        }

        // ============================================================
        // FIRMA XADES-BES CON LOGGING DETALLADO
        // ============================================================

        public async Task<string> FirmarXml(string xmlSinFirmar)
        {
            _logger.LogInformation("\n╔════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║   INICIO PROCESO DE FIRMA ELECTRÓNICA XADES-BES      ║");
            _logger.LogInformation("╚════════════════════════════════════════════════════════╝");

            var certificado = ObtenerCertificado();

            if (certificado == null)
            {
                _logger.LogWarning("⚠️ MODO SIMULACIÓN: Retornando XML sin firmar por falta de certificado.");
                return xmlSinFirmar;
            }

            ValidarCertificadoParaFirma(certificado);

            return await FirmarXml(xmlSinFirmar, certificado);
        }

        // SistemaFacturacionSRI.Infrastructure/Services/FirmaElectronicaService.cs
        // ✅ CORRECCIÓN COMPLETA: Verificación de firma con namespaces

        // Reemplazar el método FirmarXml completo con estas correcciones:

        // REEMPLAZAR el método FirmarXml(string xmlSinFirmar, X509Certificate2 certificado)

        public async Task<string> FirmarXml(string xmlSinFirmar, X509Certificate2 certificado)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("\n[INFO] Certificado: {Subject}", certificado.Subject);
                _logger.LogInformation("[INFO] Longitud XML entrada: {Length} caracteres", xmlSinFirmar.Length);

                ValidarCertificadoParaFirma(certificado);

                // ========== PASO 1: CARGAR Y VALIDAR XML ==========

                if (string.IsNullOrWhiteSpace(xmlSinFirmar))
                {
                    _logger.LogError("  ❌ XML vacío o nulo");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException("El contenido XML está vacío");
                }

                XmlDocument doc;
                try
                {
                    doc = new XmlDocument { PreserveWhitespace = true };
                    doc.LoadXml(xmlSinFirmar);
                    _logger.LogInformation("  ✓ XML parseado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  ❌ Error al parsear XML");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException("No se pudo parsear el XML", ex);
                }

                // Verificar si ya está firmado
                if (TieneFirma(xmlSinFirmar))
                {
                    _logger.LogWarning("  ⚠️ El XML ya contiene firma digital");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlYaFirmadoException();
                }

                // Obtener nodo raíz
                var nodoRaiz = doc.DocumentElement;
                if (nodoRaiz == null)
                {
                    _logger.LogError("  ❌ XML sin nodo raíz");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.XmlInvalidoException("El XML no tiene nodo raíz");
                }

                // Verificar/asignar atributo id
                var idNodo = nodoRaiz.GetAttribute("id");
                if (string.IsNullOrEmpty(idNodo))
                {
                    idNodo = "comprobante";
                    nodoRaiz.SetAttribute("id", idNodo);
                    _logger.LogInformation("  → ID asignado al nodo raíz: {Id}", idNodo);
                }

                // Validar que el id sea "comprobante"
                if (!string.Equals(idNodo, "comprobante", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("  ⚠️ El atributo 'id' del nodo raíz es '{Id}', debería ser 'comprobante'", idNodo);
                    // No lanzar excepción, solo advertir
                }

                _logger.LogInformation("  ✓ Nodo a firmar: {Nombre} (id={Id})", nodoRaiz.Name, idNodo);

                // ========== PASO 2: FIRMA XADES-BES CON FIRMAXADESNET ==========

                SignatureDocument signatureDocument = null;

                try
                {
                    signatureDocument = FirmarConFirmaXades(doc, certificado);
                    _logger.LogInformation("  ✓ Firma XAdES-BES generada exitosamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  ❌ Error al generar firma XAdES-BES");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException("Error al generar firma XAdES-BES", ex);
                }

                // Generar el XML firmado
                string xmlFirmado;
                try
                {
                    xmlFirmado = signatureDocument.Document.OuterXml;
                    _logger.LogInformation("  ✓ XML firmado generado correctamente");
                    _logger.LogInformation("    → Tamaño XML firmado: {Length} caracteres", xmlFirmado.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  ❌ Error al generar el XML firmado");
                    throw new SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException("Error al generar XML firmado", ex);
                }

                var tiempoTranscurrido = DateTime.Now - startTime;
                _logger.LogInformation("⏱️  Tiempo total de firma: {Tiempo}ms", tiempoTranscurrido.TotalMilliseconds);

                return await Task.FromResult(xmlFirmado);
            }
            catch (SistemaFacturacionSRI.Domain.Exceptions.XmlYaFirmadoException)
            {
                // Re-lanzar excepciones de dominio tal cual
                throw;
            }
            catch (SistemaFacturacionSRI.Domain.Exceptions.ErrorFirmaException)
            {
                // Re-lanzar excepciones de dominio tal cual
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al firmar XML: {Message}", ex.Message);
                throw new Exception("Error al firmar XML", ex);
            }
        }

        // REEMPLAZAR COMPLETAMENTE el método FirmarConFirmaXades
        // Basado en código funcional que usa parametros.Signer correctamente

        private SignatureDocument FirmarConFirmaXades(XmlDocument xmlDoc, X509Certificate2 certificado)
        {
            _logger.LogInformation("  [FIRMA] Iniciando firma XAdES-BES con FirmaXadesNet...");

            // ========== DIAGNÓSTICO DEL CERTIFICADO ==========
            _logger.LogInformation("  [DIAGNÓSTICO] Analizando certificado:");
            _logger.LogInformation("    → Subject: {Subject}", certificado.Subject);
            _logger.LogInformation("    → Issuer: {Issuer}", certificado.Issuer);
            _logger.LogInformation("    → HasPrivateKey: {HasKey}", certificado.HasPrivateKey);
            _logger.LogInformation("    → NotBefore: {NotBefore:yyyy-MM-dd HH:mm:ss}", certificado.NotBefore);
            _logger.LogInformation("    → NotAfter: {NotAfter:yyyy-MM-dd HH:mm:ss}", certificado.NotAfter);

            try
            {
                var rsa = certificado.GetRSAPrivateKey();
                if (rsa != null)
                {
                    _logger.LogInformation("    → RSA Key Size: {KeySize} bits", rsa.KeySize);
                    _logger.LogInformation("    ✓ Clave privada RSA accesible");
                }
                else
                {
                    _logger.LogError("    ❌ No se pudo obtener la clave RSA");
                    throw new InvalidOperationException("No se pudo obtener la clave privada RSA del certificado.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "    ❌ Error al acceder a clave privada");
                throw;
            }

            // ========== INTENTAR FIRMA CON SHA256, FALLBACK A SHA1 ==========
            SignatureDocument signatureDocument = null;

            try
            {
                _logger.LogInformation("  [FIRMA] Intentando con RSA-SHA256 / SHA256...");
                signatureDocument = FirmarConAlgoritmo(
                    xmlDoc,
                    certificado,
                    SignatureMethod.RSAwithSHA256,
                    DigestMethod.SHA256);

                _logger.LogInformation("  [FIRMA] ✓ Firma generada exitosamente con RSA-SHA256 / SHA256");
            }
            catch (CryptographicException cex)
            {
                // Error típico cuando el entorno no soporta SHA256 para XMLDSIG
                if (cex.Message != null &&
                    cex.Message.IndexOf("Cryptography_Xml_SignatureDescriptionNotCreated",
                                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogWarning("  [FIRMA] ⚠️ Entorno no soporta RSA-SHA256, usando fallback a SHA1");
                    _logger.LogWarning("    Mensaje: {Message}", cex.Message);

                    signatureDocument = FirmarConAlgoritmo(
                        xmlDoc,
                        certificado,
                        SignatureMethod.RSAwithSHA1,
                        DigestMethod.SHA1);

                    _logger.LogInformation("  [FIRMA] ✓ Firma generada con RSA-SHA1 / SHA1 (modo compatibilidad)");
                }
                else
                {
                    _logger.LogError(cex, "  [FIRMA] ❌ Error criptográfico no manejado");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "  [FIRMA] ❌ Error al intentar SHA256");

                // Intentar con SHA1 como último recurso
                try
                {
                    _logger.LogInformation("  [FIRMA] Intentando fallback a RSA-SHA1 / SHA1...");
                    signatureDocument = FirmarConAlgoritmo(
                        xmlDoc,
                        certificado,
                        SignatureMethod.RSAwithSHA1,
                        DigestMethod.SHA1);

                    _logger.LogInformation("  [FIRMA] ✓ Firma generada con RSA-SHA1 / SHA1 (fallback)");
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "  [FIRMA] ❌ También falló con SHA1");
                    throw;
                }
            }

            if (signatureDocument == null || signatureDocument.Document == null)
            {
                _logger.LogError("  [FIRMA] ❌ No se pudo generar el documento firmado");
                throw new InvalidOperationException("No se pudo generar el documento firmado.");
            }

            // ========== VERIFICAR ESTRUCTURA DE FIRMA ==========
            _logger.LogInformation("  [FIRMA] Verificando estructura de firma...");

            bool tieneSignature = signatureDocument.Document
                .GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").Count > 0;
            bool tieneQualifying = signatureDocument.Document
                .GetElementsByTagName("QualifyingProperties", "http://uri.etsi.org/01903/v1.3.2#").Count > 0;

            _logger.LogInformation("    → Contiene <Signature>: {HasSig}", tieneSignature);
            _logger.LogInformation("    → Contiene <QualifyingProperties>: {HasQual}", tieneQualifying);

            if (!tieneSignature)
            {
                _logger.LogError("  [FIRMA] ❌ El documento firmado NO contiene elemento <Signature>");
                throw new InvalidOperationException("El documento firmado no contiene elemento <Signature>");
            }

            _logger.LogInformation("  [FIRMA] ✓ Estructura de firma XAdES-BES correcta");

            return signatureDocument;
        }

        /// <summary>
        /// Realiza la firma XAdES-BES con el algoritmo indicado.
        /// CLAVE: Usa parametros.Signer = new Signer(certificado) DENTRO del using
        /// </summary>
        private SignatureDocument FirmarConAlgoritmo(
            XmlDocument xmlDoc,
            X509Certificate2 certificado,
            SignatureMethod signatureMethod,
            DigestMethod digestMethod)
        {
            var xadesService = new XadesService();

            var parametros = new SignatureParameters
            {
                SigningDate = DateTime.Now,
                SignatureMethod = signatureMethod,
                DigestMethod = digestMethod,
                SignaturePackaging = SignaturePackaging.ENVELOPED,
                ElementIdToSign = "comprobante",
                ExternalContentUri = string.Empty
            };

            // Configurar formato de datos
            parametros.DataFormat = new DataFormat
            {
                MimeType = "text/xml"
            };

            // Compromiso de firma (opcional pero correcto para SRI)
            parametros.SignatureCommitments.Add(
                new SignatureCommitment(SignatureCommitmentType.ProofOfOrigin));

            // ⭐ CLAVE: Asignar el Signer DENTRO de parametros y usar using
            using (parametros.Signer = new Signer(certificado))
            using (var ms = new MemoryStream())
            {
                xmlDoc.Save(ms);
                ms.Position = 0;

                _logger.LogDebug("  [FIRMA] Stream XML: {Length} bytes", ms.Length);
                _logger.LogDebug("  [FIRMA] Método: {Method}, Digest: {Digest}",
                    signatureMethod, digestMethod);

                // Realizar la firma
                return xadesService.Sign(ms, parametros);
            }
        }
        // ============================================================
        // MÉTODOS DE VALIDACIÓN Y UTILIDADES
        // ============================================================

        public async Task<bool> ValidarFirma(string xmlFirmado)
        {
            var resultado = await ValidarFirmaDetallada(xmlFirmado);
            return resultado.EsValida;
        }

        public async Task<ResultadoValidacionFirma> ValidarFirmaDetallada(string xmlFirmado)
        {
            _logger.LogInformation("Validando firma de XML");

            try
            {
                if (!TieneFirma(xmlFirmado))
                {
                    return ResultadoValidacionFirma.ConError("El XML no tiene firma digital");
                }

                var doc = new XmlDocument();
                doc.LoadXml(xmlFirmado);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
                nsmgr.AddNamespace("etsi", "http://uri.etsi.org/01903/v1.3.2#");

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

                var signedInfo = doc.SelectSingleNode("//ds:SignedInfo", nsmgr) as XmlElement;
                if (signedInfo == null)
                {
                    return ResultadoValidacionFirma.ConError("No se encontró SignedInfo");
                }

                var signatureValue = doc.SelectSingleNode("//ds:SignatureValue", nsmgr);
                if (signatureValue == null || string.IsNullOrWhiteSpace(signatureValue.InnerText))
                {
                    return ResultadoValidacionFirma.ConError("No se encontró SignatureValue");
                }

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

        public bool TieneFirma(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                var signatureNode = doc.SelectSingleNode("//ds:Signature", nsmgr);
                return signatureNode != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando si XML tiene firma");
                return false;
            }
        }

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

        public X509Certificate2 ObtenerCertificadoConfiguracion()
        {
            return ObtenerCertificado();
        }
    }
}