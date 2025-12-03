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
        private X509Certificate2? _certificadoActual;
        private readonly object _lock = new object();

        public CertificadoDigitalService(
            IOptions<CertificadoDigitalOptions> options,
            ILogger<CertificadoDigitalService> logger)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validar configuración al inicializar
            _options.Validar();
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

                _logger.LogInformation("Cargando certificado digital desde configuración");

                var rutaAbsoluta = _options.ObtenerRutaAbsoluta();
                _certificadoActual = CargarCertificado(rutaAbsoluta, _options.ClaveCertificado);

                return _certificadoActual;
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
    }
}