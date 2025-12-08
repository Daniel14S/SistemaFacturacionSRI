using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaFacturacionSRI.Domain.Configuration;
using SistemaFacturacionSRI.Domain.Interfaces.Services;

namespace SistemaFacturacionSRI.Infrastructure.Services
{
    /// <summary>
    /// Servicio para cargar y gestionar certificados digitales
    /// T-053, T-054: Sprint 3 - Día 6
    /// </summary>
    public class CertificadoDigitalService : ICertificadoDigitalService
    {
        private readonly CertificadoDigitalOptions _options;
        private readonly ILogger<CertificadoDigitalService> _logger;
        private readonly ICertificadoDigitalStorageService _storageService;
        private X509Certificate2? _certificadoActual;
        private readonly object _lock = new object();

        public CertificadoDigitalService(
            IOptions<CertificadoDigitalOptions> options,
            ILogger<CertificadoDigitalService> logger,
            ICertificadoDigitalStorageService storageService)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            // Validar configuración al inicializar
            try 
            {
                _options.Validar();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "La configuración del certificado digital no es válida. El servicio funcionará en modo limitado.");
            }
        }

        public string ObtenerPassword()
        {
            return _options.ClaveCertificado;
        }

        /// <summary>
        /// Carga el certificado desde la configuración (thread-safe)
        /// </summary>
        public X509Certificate2 CargarCertificado()
        {
            lock (_lock)
            {
                if (_certificadoActual != null)
                {
                    _logger.LogDebug("Retornando certificado ya cargado");
                    return _certificadoActual;
                }

                // 1) Intentar cargar desde almacenamiento seguro en BD
                var certificadoBd = _storageService.ObtenerCertificadoActivoAsync().GetAwaiter().GetResult();
                if (certificadoBd != null)
                {
                    _logger.LogInformation("Cargando certificado digital desde almacenamiento seguro en base de datos");
                    _certificadoActual = CargarCertificado(certificadoBd.ArchivoBytes, certificadoBd.ClavePlano);
                    return _certificadoActual;
                }

                // 2) Fallback a configuración de archivos
                _logger.LogInformation("Cargando certificado digital desde configuración de archivos");

                var rutaAbsoluta = _options.ObtenerRutaAbsoluta();
                _certificadoActual = CargarCertificado(rutaAbsoluta, _options.ClaveCertificado);

                return _certificadoActual;
            }
        }

        /// <summary>
        /// Carga el certificado desde bytes y contraseña específicas
        /// </summary>
        private X509Certificate2 CargarCertificado(byte[] contenido, string password)
        {
            try
            {
                _logger.LogInformation("Cargando certificado desde almacenamiento seguro (bytes)");

                var certificado = new X509Certificate2(
                    contenido,
                    password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                );

                _logger.LogInformation("Certificado cargado exitosamente desde BD. Subject: {Subject}",
                    certificado.Subject);

                if (_options.ValidarVigencia && !ValidarCertificado(certificado))
                {
                    throw new InvalidOperationException("El certificado no es válido o está expirado");
                }

                if (!TieneClavePrivada(certificado))
                {
                    throw new InvalidOperationException("El certificado no contiene clave privada.");
                }

                return certificado;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Error de criptografía al cargar certificado desde BD");
                throw new InvalidOperationException(
                    "No se pudo cargar el certificado almacenado. Verifique la clave.",
                    ex);
            }
        }

        /// <summary>
        /// Carga el certificado desde ruta y contraseña específicas
        /// </summary>
        public X509Certificate2 CargarCertificado(string rutaCertificado, string password)
        {
            try
            {
                _logger.LogInformation("Cargando certificado desde: {Ruta}", rutaCertificado);

                // Verificar que el archivo existe
                if (!File.Exists(rutaCertificado))
                {
                    throw new FileNotFoundException(
                        $"No se encontró el archivo del certificado en: {rutaCertificado}");
                }

                // Cargar el certificado
                var certificado = new X509Certificate2(
                    rutaCertificado,
                    password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                );

                _logger.LogInformation("Certificado cargado exitosamente. Subject: {Subject}", 
                    certificado.Subject);

                // Validar el certificado si está configurado
                if (_options.ValidarVigencia)
                {
                    if (!ValidarCertificado(certificado))
                    {
                        throw new InvalidOperationException(
                            "El certificado no es válido o está expirado");
                    }
                }

                // Verificar que tiene clave privada
                if (!TieneClavePrivada(certificado))
                {
                    throw new InvalidOperationException(
                        "El certificado no contiene clave privada. " +
                        "Se requiere un certificado con clave privada para firmar documentos.");
                }

                return certificado;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Error de criptografía al cargar certificado");
                throw new InvalidOperationException(
                    "No se pudo cargar el certificado. Verifique la contraseña y el formato del archivo.", 
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar certificado desde {Ruta}", rutaCertificado);
                throw;
            }
        }

        /// <summary>
        /// Valida que el certificado sea válido
        /// </summary>
        public bool ValidarCertificado(X509Certificate2 certificado)
        {
            try
            {
                _logger.LogDebug("Validando certificado: {Subject}", certificado.Subject);

                // 1. Verificar que no esté expirado
                if (EstaExpirado(certificado))
                {
                    _logger.LogWarning("El certificado está expirado. Válido hasta: {FechaExpiracion}",
                        certificado.NotAfter);
                    return false;
                }

                // 2. Verificar que ya sea válido (no antes de NotBefore)
                if (DateTime.Now < certificado.NotBefore)
                {
                    _logger.LogWarning("El certificado aún no es válido. Válido desde: {FechaInicio}",
                        certificado.NotBefore);
                    return false;
                }

                // 3. Verificar cadena de confianza (opcional)
                if (_options.ValidarCadenaConfianza)
                {
                    using var chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.VerificationTime = DateTime.Now;

                    bool chainIsValid = chain.Build(certificado);

                    if (!chainIsValid)
                    {
                        _logger.LogWarning("La cadena de confianza del certificado no es válida");
                        foreach (var chainStatus in chain.ChainStatus)
                        {
                            _logger.LogWarning("Estado de cadena: {Status} - {Info}",
                                chainStatus.Status, chainStatus.StatusInformation);
                        }
                        return false;
                    }
                }

                // 4. Verificar que tenga uso de firma digital
                var keyUsages = ObtenerKeyUsages(certificado);
                if (!keyUsages.Contains("Digital Signature") && !keyUsages.Contains("Non Repudiation"))
                {
                    _logger.LogWarning("El certificado no tiene el uso 'Digital Signature' habilitado");
                    // No retornar false aquí porque algunos certificados válidos pueden no especificarlo
                }

                _logger.LogInformation("Certificado válido. Expira en {Dias} días",
                    (certificado.NotAfter - DateTime.Now).Days);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar certificado");
                return false;
            }
        }

        /// <summary>
        /// Obtiene información detallada del certificado
        /// </summary>
        public InformacionCertificado ObtenerInformacionCertificado(X509Certificate2 certificado)
        {
            var diasRestantes = (certificado.NotAfter - DateTime.Now).Days;

            return new InformacionCertificado
            {
                Subject = certificado.Subject,
                Issuer = certificado.Issuer,
                SerialNumber = certificado.SerialNumber,
                Thumbprint = certificado.Thumbprint,
                ValidoDesde = certificado.NotBefore,
                ValidoHasta = certificado.NotAfter,
                TieneClavePrivada = TieneClavePrivada(certificado),
                EsValido = ValidarCertificado(certificado),
                DiasRestantes = diasRestantes > 0 ? diasRestantes : 0,
                SignatureAlgorithm = certificado.SignatureAlgorithm.FriendlyName ?? "Desconocido",
                Version = certificado.Version,
                KeyUsages = ObtenerKeyUsages(certificado),
                ExtendedKeyUsages = ObtenerExtendedKeyUsages(certificado)
            };
        }

        /// <summary>
        /// Verifica si el certificado tiene clave privada
        /// </summary>
        public bool TieneClavePrivada(X509Certificate2 certificado)
        {
            return certificado.HasPrivateKey;
        }

        /// <summary>
        /// Verifica si el certificado está expirado
        /// </summary>
        public bool EstaExpirado(X509Certificate2 certificado)
        {
            return DateTime.Now > certificado.NotAfter;
        }

        /// <summary>
        /// Obtiene el certificado actualmente cargado
        /// </summary>
        public X509Certificate2 ObtenerCertificadoActual()
        {
            if (_certificadoActual == null)
            {
                return CargarCertificado();
            }

            return _certificadoActual;
        }

        /// <summary>
        /// Obtiene los usos de clave del certificado
        /// </summary>
        private List<string> ObtenerKeyUsages(X509Certificate2 certificado)
        {
            var keyUsages = new List<string>();

            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsageExt)
                {
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
                        keyUsages.Add("Digital Signature");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation))
                        keyUsages.Add("Non Repudiation");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.KeyEncipherment))
                        keyUsages.Add("Key Encipherment");
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DataEncipherment))
                        keyUsages.Add("Data Encipherment");
                }
            }

            return keyUsages;
        }

        /// <summary>
        /// Obtiene los usos extendidos de clave del certificado
        /// </summary>
        private List<string> ObtenerExtendedKeyUsages(X509Certificate2 certificado)
        {
            var extendedKeyUsages = new List<string>();

            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509EnhancedKeyUsageExtension ekuExt)
                {
                    foreach (var oid in ekuExt.EnhancedKeyUsages)
                    {
                        extendedKeyUsages.Add(oid.FriendlyName ?? oid.Value ?? "Desconocido");
                    }
                }
            }

            return extendedKeyUsages;
        }

        // AGREGAR estos métodos adicionales a CertificadoDigitalService.cs
        // Infrastructure/Services/CertificadoDigitalService.cs

        // ============================================================
        // T-054: VALIDACIONES ADICIONALES DE CERTIFICADO
        // ============================================================

        /// <summary>
        /// T-054: Validación completa y estricta del certificado para firma SRI
        /// </summary>
        public (bool EsValido, List<string> Errores, List<string> Advertencias) ValidarCertificadoCompleto(
            X509Certificate2 certificado)
        {
            var errores = new List<string>();
            var advertencias = new List<string>();

            _logger.LogInformation("Iniciando validación completa del certificado");

            // 1. Validar vigencia temporal
            ValidarVigenciaTemporal(certificado, errores, advertencias);

            // 2. Validar clave privada
            ValidarClavePrivada(certificado, errores);

            // 3. Validar algoritmo y tamaño de clave
            ValidarAlgoritmoYTamanoClave(certificado, errores, advertencias);

            // 4. Validar usos de clave (Key Usage)
            ValidarKeyUsage(certificado, errores, advertencias);

            // 5. Validar cadena de confianza (si está configurado)
            if (_options.ValidarCadenaConfianza)
            {
                ValidarCadenaConfianza(certificado, errores, advertencias);
            }

            // 6. Validar que sea certificado de persona (no de servidor/CA)
            ValidarTipoCertificado(certificado, errores, advertencias);

            // 7. Validar emisor autorizado en Ecuador
            ValidarEmisorAutorizado(certificado, advertencias);

            // 8. Validar extensiones críticas
            ValidarExtensionesCriticas(certificado, advertencias);

            var esValido = errores.Count == 0;
            
            if (esValido)
            {
                _logger.LogInformation("✅ Certificado válido para firma electrónica SRI");
            }
            else
            {
                _logger.LogError("❌ Certificado NO válido. Errores: {Count}", errores.Count);
            }

            return (esValido, errores, advertencias);
        }

        /// <summary>
        /// Valida que el certificado esté dentro de su período de vigencia
        /// </summary>
        private void ValidarVigenciaTemporal(
            X509Certificate2 certificado,
            List<string> errores,
            List<string> advertencias)
        {
            var ahora = DateTime.Now;

            // Verificar que ya sea válido
            if (ahora < certificado.NotBefore)
            {
                errores.Add($"El certificado aún no es válido. Será válido desde: {certificado.NotBefore:dd/MM/yyyy HH:mm:ss}");
                _logger.LogError("Certificado aún no válido. NotBefore: {NotBefore}", certificado.NotBefore);
                return;
            }

            // Verificar que no esté expirado
            if (ahora > certificado.NotAfter)
            {
                errores.Add($"El certificado está EXPIRADO. Expiró el: {certificado.NotAfter:dd/MM/yyyy HH:mm:ss}");
                _logger.LogError("Certificado expirado. NotAfter: {NotAfter}", certificado.NotAfter);
                return;
            }

            // Advertir si está por expirar
            var diasRestantes = (certificado.NotAfter - ahora).Days;
            
            if (diasRestantes <= 7)
            {
                advertencias.Add($"⚠️ URGENTE: El certificado expira en {diasRestantes} días. Renuévelo inmediatamente.");
                _logger.LogWarning("Certificado expira en {Dias} días", diasRestantes);
            }
            else if (diasRestantes <= 30)
            {
                advertencias.Add($"El certificado expira en {diasRestantes} días. Considere renovarlo pronto.");
                _logger.LogInformation("Certificado expira en {Dias} días", diasRestantes);
            }

            _logger.LogDebug("Vigencia válida. Días restantes: {Dias}", diasRestantes);
        }

        /// <summary>
        /// Valida que el certificado tenga clave privada
        /// </summary>
        private void ValidarClavePrivada(X509Certificate2 certificado, List<string> errores)
        {
            if (!certificado.HasPrivateKey)
            {
                errores.Add("El certificado NO contiene clave privada. Se requiere clave privada para firmar documentos.");
                _logger.LogError("Certificado sin clave privada");
                return;
            }

            // Intentar acceder a la clave privada
            try
            {
                using var rsa = certificado.GetRSAPrivateKey();
                if (rsa == null)
                {
                    errores.Add("No se pudo acceder a la clave privada del certificado.");
                    _logger.LogError("Clave privada inaccesible");
                }
            }
            catch (Exception ex)
            {
                errores.Add($"Error al acceder a la clave privada: {ex.Message}");
                _logger.LogError(ex, "Error accediendo a clave privada");
            }
        }

        /// <summary>
        /// Valida el algoritmo de firma y el tamaño de clave
        /// </summary>
        private void ValidarAlgoritmoYTamanoClave(
            X509Certificate2 certificado,
            List<string> errores,
            List<string> advertencias)
        {
            // Validar algoritmo de firma del certificado
            var signatureAlgorithm = certificado.SignatureAlgorithm.FriendlyName ?? "Desconocido";
            
            if (!signatureAlgorithm.Contains("RSA") && !signatureAlgorithm.Contains("sha"))
            {
                advertencias.Add($"Algoritmo de firma poco común: {signatureAlgorithm}");
                _logger.LogWarning("Algoritmo de firma: {Algorithm}", signatureAlgorithm);
            }

            // Validar tamaño de clave
            try
            {
                using var rsa = certificado.GetRSAPrivateKey();
                if (rsa != null)
                {
                    var keySize = rsa.KeySize;

                    if (keySize < 2048)
                    {
                        errores.Add($"El tamaño de clave ({keySize} bits) es inseguro. Se requiere mínimo 2048 bits.");
                        _logger.LogError("Tamaño de clave insuficiente: {KeySize} bits", keySize);
                    }
                    else if (keySize == 2048)
                    {
                        _logger.LogInformation("Tamaño de clave: {KeySize} bits (mínimo aceptable)", keySize);
                    }
                    else
                    {
                        _logger.LogInformation("Tamaño de clave: {KeySize} bits (bueno)", keySize);
                    }
                }
            }
            catch (Exception ex)
            {
                advertencias.Add($"No se pudo verificar el tamaño de clave: {ex.Message}");
                _logger.LogWarning(ex, "Error verificando tamaño de clave");
            }
        }

        /// <summary>
        /// Valida los usos de clave (Key Usage) del certificado
        /// </summary>
        private void ValidarKeyUsage(
            X509Certificate2 certificado,
            List<string> errores,
            List<string> advertencias)
        {
            bool tieneDigitalSignature = false;
            bool tieneNonRepudiation = false;

            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509KeyUsageExtension keyUsageExt)
                {
                    tieneDigitalSignature = keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);
                    tieneNonRepudiation = keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation);

                    _logger.LogDebug("Key Usage encontrado - DigitalSignature: {DS}, NonRepudiation: {NR}",
                        tieneDigitalSignature, tieneNonRepudiation);

                    // Verificar que NO tenga usos incompatibles con firma
                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.KeyCertSign))
                    {
                        advertencias.Add("El certificado tiene uso 'KeyCertSign' (típico de CA). Asegúrese de que sea el certificado correcto.");
                    }

                    if (keyUsageExt.KeyUsages.HasFlag(X509KeyUsageFlags.CrlSign))
                    {
                        advertencias.Add("El certificado tiene uso 'CrlSign' (típico de CA). Asegúrese de que sea el certificado correcto.");
                    }

                    break;
                }
            }

            // Validar que tenga al menos uno de los usos necesarios para firma
            if (!tieneDigitalSignature && !tieneNonRepudiation)
            {
                errores.Add("El certificado NO tiene los usos 'Digital Signature' o 'Non Repudiation'. No es válido para firma electrónica.");
                _logger.LogError("Certificado sin usos de firma digital");
            }
        }

        /// <summary>
        /// Valida la cadena de confianza del certificado
        /// </summary>
        private void ValidarCadenaConfianza(
            X509Certificate2 certificado,
            List<string> errores,
            List<string> advertencias)
        {
            _logger.LogInformation("Validando cadena de confianza del certificado");

            try
            {
                using var chain = new X509Chain();
                
                // Configuración estricta para producción
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                chain.ChainPolicy.VerificationTime = DateTime.Now;
                chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(30);

                bool isValid = chain.Build(certificado);

                if (!isValid)
                {
                    errores.Add("La cadena de confianza del certificado no es válida.");
                    
                    foreach (var chainStatus in chain.ChainStatus)
                    {
                        var mensaje = $"  • {chainStatus.Status}: {chainStatus.StatusInformation}";
                        
                        // Algunos errores son críticos, otros son advertencias
                        if (EsErrorCriticoCadena(chainStatus.Status))
                        {
                            errores.Add(mensaje);
                            _logger.LogError("Error crítico en cadena: {Status}", chainStatus.Status);
                        }
                        else
                        {
                            advertencias.Add(mensaje);
                            _logger.LogWarning("Advertencia en cadena: {Status}", chainStatus.Status);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("✅ Cadena de confianza válida");
                }
            }
            catch (Exception ex)
            {
                advertencias.Add($"No se pudo validar la cadena de confianza: {ex.Message}");
                _logger.LogWarning(ex, "Error validando cadena de confianza");
            }
        }

        /// <summary>
        /// Determina si un error de cadena es crítico
        /// </summary>
        private bool EsErrorCriticoCadena(X509ChainStatusFlags status)
        {
            return status switch
            {
                X509ChainStatusFlags.NotTimeValid => true,
                X509ChainStatusFlags.NotTimeNested => true,
                X509ChainStatusFlags.Revoked => true,
                X509ChainStatusFlags.NotSignatureValid => true,
                X509ChainStatusFlags.NotValidForUsage => true,
                X509ChainStatusFlags.UntrustedRoot => false, // Común en desarrollo
                X509ChainStatusFlags.PartialChain => false,  // Puede ser aceptable
                X509ChainStatusFlags.RevocationStatusUnknown => false, // Común si no hay conexión
                X509ChainStatusFlags.OfflineRevocation => false,
                _ => false
            };
        }

        /// <summary>
        /// Valida que sea un certificado de persona (no CA o servidor)
        /// </summary>
        private void ValidarTipoCertificado(
            X509Certificate2 certificado,
            List<string> errores,
            List<string> advertencias)
        {
            // Verificar Basic Constraints
            foreach (var extension in certificado.Extensions)
            {
                if (extension is X509BasicConstraintsExtension basicConstraints)
                {
                    if (basicConstraints.CertificateAuthority)
                    {
                        errores.Add("Este es un certificado de Autoridad Certificadora (CA), no un certificado de firma personal.");
                        _logger.LogError("Certificado es CA");
                        return;
                    }
                }
            }

            // Verificar que el Subject tenga información de persona
            var subject = certificado.Subject;
            if (!subject.Contains("CN=") && !subject.Contains("SERIALNUMBER="))
            {
                advertencias.Add("El certificado no parece ser de una persona física. Verifique que sea el certificado correcto.");
                _logger.LogWarning("Subject inusual: {Subject}", subject);
            }
        }

        /// <summary>
        /// Valida que el emisor sea una CA autorizada en Ecuador
        /// </summary>
        private void ValidarEmisorAutorizado(X509Certificate2 certificado, List<string> advertencias)
        {
            var issuer = certificado.Issuer.ToUpper();
            
            // Entidades Certificadoras autorizadas en Ecuador
            var emisoresAutorizados = new[]
            {
                "SECURITY DATA",
                "BANCO CENTRAL",
                "ANF",
                "CONSEJO DE LA JUDICATURA",
                "BCE", // Banco Central del Ecuador
                "UANATACA"
            };

            bool esEmisorAutorizado = emisoresAutorizados.Any(e => issuer.Contains(e));

            if (!esEmisorAutorizado)
            {
                advertencias.Add($"El emisor '{certificado.Issuer}' no está en la lista de CAs conocidas autorizadas en Ecuador. Verifique que sea válido.");
                _logger.LogWarning("Emisor no reconocido: {Issuer}", certificado.Issuer);
            }
            else
            {
                _logger.LogInformation("Emisor autorizado: {Issuer}", certificado.Issuer);
            }
        }

        /// <summary>
        /// Valida extensiones críticas del certificado
        /// </summary>
        private void ValidarExtensionesCriticas(X509Certificate2 certificado, List<string> advertencias)
        {
            var extensionesCriticas = certificado.Extensions
                .Where(e => e.Critical)
                .ToList();

            if (extensionesCriticas.Any())
            {
                _logger.LogDebug("Extensiones críticas encontradas: {Count}", extensionesCriticas.Count);
                
                foreach (var ext in extensionesCriticas)
                {
                    var nombre = ext.Oid?.FriendlyName ?? ext.Oid?.Value ?? "Desconocida";
                    _logger.LogDebug("  Extensión crítica: {Extension}", nombre);
                }
            }
        }

        /// <summary>
        /// Método público para validar y registrar resultados
        /// </summary>
        public bool ValidarYRegistrarCertificado(X509Certificate2 certificado)
        {
            var (esValido, errores, advertencias) = ValidarCertificadoCompleto(certificado);

            // Registrar errores
            foreach (var error in errores)
            {
                _logger.LogError("❌ {Error}", error);
            }

            // Registrar advertencias
            foreach (var advertencia in advertencias)
            {
                _logger.LogWarning("⚠️ {Advertencia}", advertencia);
            }

            // Registrar resumen
            if (esValido)
            {
                _logger.LogInformation("═══════════════════════════════════════");
                _logger.LogInformation("✅ CERTIFICADO VÁLIDO PARA FIRMA SRI");
                _logger.LogInformation("═══════════════════════════════════════");
                if (advertencias.Any())
                {
                    _logger.LogInformation("Advertencias: {Count}", advertencias.Count);
                }
            }
            else
            {
                _logger.LogError("═══════════════════════════════════════");
                _logger.LogError("❌ CERTIFICADO NO VÁLIDO");
                _logger.LogError("Errores: {ErrorCount} | Advertencias: {WarnCount}",
                    errores.Count, advertencias.Count);
                _logger.LogError("═══════════════════════════════════════");
            }

            return esValido;
        }

        public void RefrescarCertificado()
        {
            lock (_lock)
            {
                _certificadoActual = null;
            }
            _logger.LogInformation("Caché de certificado limpiada; se recargará en el próximo uso.");
        }

    }
}