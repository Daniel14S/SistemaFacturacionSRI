using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SistemaFacturacionSRI.Application.Services
{
    /// <summary>
    /// Gestiona la carga y validación del certificado digital .p12
    /// </summary>
    public class CertificadoDigitalLoader
    {
        private X509Certificate2? _certificado;
        private readonly string _rutaCertificado;
        private readonly string _claveCertificado;

        public CertificadoDigitalLoader(string rutaCertificado, string claveCertificado)
        {
            _rutaCertificado = rutaCertificado ?? throw new ArgumentNullException(nameof(rutaCertificado));
            _claveCertificado = claveCertificado ?? throw new ArgumentNullException(nameof(claveCertificado));
        }

        /// <summary>
        /// Carga el certificado .p12 desde el archivo
        /// </summary>
        public X509Certificate2 CargarCertificado()
        {
            try
            {
                // Validar que el archivo existe
                if (!File.Exists(_rutaCertificado))
                {
                    throw new FileNotFoundException($"No se encontró el certificado en: {_rutaCertificado}");
                }

                // Cargar certificado con clave privada
                _certificado = new X509Certificate2(
                    _rutaCertificado,
                    _claveCertificado,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
                );

                // Validar que tiene clave privada
                if (!_certificado.HasPrivateKey)
                {
                    throw new InvalidOperationException("El certificado no contiene clave privada");
                }

                // Validar certificado
                ValidarCertificado(_certificado);

                return _certificado;
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Error al cargar el certificado. Verifique que la contraseña sea correcta.", ex);
            }
        }

        /// <summary>
        /// T-54: Valida que el certificado sea válido para firma
        /// </summary>
        private void ValidarCertificado(X509Certificate2 cert)
        {
            // 1. Verificar que no esté expirado
            var ahora = DateTime.Now;
            if (ahora < cert.NotBefore || ahora > cert.NotAfter)
            {
                throw new InvalidOperationException(
                    $"Certificado expirado o no válido. Válido desde {cert.NotBefore:dd/MM/yyyy} hasta {cert.NotAfter:dd/MM/yyyy}");
            }

            // 2. Verificar que sea un certificado de firma digital
            var keyUsages = cert.Extensions
                .OfType<X509KeyUsageExtension>()
                .FirstOrDefault();

            if (keyUsages != null)
            {
                bool tieneFirmaDigital = keyUsages.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) ||
                                        keyUsages.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation);
                
                if (!tieneFirmaDigital)
                {
                    throw new InvalidOperationException("El certificado no está habilitado para firma digital");
                }
            }

            // 3. Verificar que tenga algoritmo RSA
            if (cert.GetRSAPrivateKey() == null)
            {
                throw new InvalidOperationException("El certificado debe usar algoritmo RSA");
            }
        }

        /// <summary>
        /// Verifica la cadena de confianza del certificado
        /// </summary>
        public bool VerificarCadenaConfianza()
        {
            if (_certificado == null)
            {
                throw new InvalidOperationException("Debe cargar el certificado primero");
            }

            var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = X509RevocationMode.Online,
                    RevocationFlag = X509RevocationFlag.ExcludeRoot,
                    VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority
                }
            };

            return chain.Build(_certificado);
        }

        /// <summary>
        /// Obtiene información legible del certificado
        /// </summary>
        public InformacionCertificado ObtenerInformacion()
        {
            if (_certificado == null)
            {
                throw new InvalidOperationException("Debe cargar el certificado primero");
            }

            return new InformacionCertificado
            {
                Emisor = _certificado.Issuer,
                Sujeto = _certificado.Subject,
                NumeroSerie = _certificado.SerialNumber,
                FechaEmision = _certificado.NotBefore,
                FechaExpiracion = _certificado.NotAfter,
                Huella = _certificado.Thumbprint,
                EstaVigente = DateTime.Now >= _certificado.NotBefore && DateTime.Now <= _certificado.NotAfter,
                TieneClavePrivada = _certificado.HasPrivateKey,
                Algoritmo = _certificado.SignatureAlgorithm.FriendlyName ?? "Desconocido"
            };
        }

        /// <summary>
        /// Obtiene el certificado cargado
        /// </summary>
        public X509Certificate2 ObtenerCertificado()
        {
            if (_certificado == null)
            {
                return CargarCertificado();
            }
            return _certificado;
        }
    }

    /// <summary>
    /// DTO con información del certificado
    /// </summary>
    public class InformacionCertificado
    {
        public string Emisor { get; set; } = string.Empty;
        public string Sujeto { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public string Huella { get; set; } = string.Empty;
        public bool EstaVigente { get; set; }
        public bool TieneClavePrivada { get; set; }
        public string Algoritmo { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"""
                Certificado Digital:
                - Sujeto: {Sujeto}
                - Emisor: {Emisor}
                - Serie: {NumeroSerie}
                - Válido desde: {FechaEmision:dd/MM/yyyy}
                - Válido hasta: {FechaExpiracion:dd/MM/yyyy}
                - Estado: {(EstaVigente ? "VIGENTE" : "EXPIRADO")}
                - Clave privada: {(TieneClavePrivada ? "SÍ" : "NO")}
                - Algoritmo: {Algoritmo}
                - Huella SHA1: {Huella}
                """;
        }
    }
}