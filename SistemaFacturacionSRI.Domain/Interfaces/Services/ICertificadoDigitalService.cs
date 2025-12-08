// SistemaFacturacionSRI.Domain/Interfaces/Services/ICertificadoDigitalService.cs
using System.Security.Cryptography.X509Certificates;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    /// <summary>
    /// Servicio para gestionar certificados digitales
    /// T-053: Sprint 3 - Día 6
    /// </summary>
    public interface ICertificadoDigitalService
    {
        /// <summary>
        /// Carga el certificado digital desde la configuración
        /// </summary>
        /// <returns>Certificado X509 cargado</returns>
        X509Certificate2 CargarCertificado();

        /// <summary>
        /// Obtiene el password del certificado para operaciones de exportación
        /// </summary>
        string ObtenerPassword();
        /// <summary>
        /// Carga el certificado desde una ruta específica
        /// </summary>
        /// <param name="rutaCertificado">Ruta al archivo .p12/.pfx</param>
        /// <param name="password">Contraseña del certificado</param>
        /// <returns>Certificado X509 cargado</returns>
        X509Certificate2 CargarCertificado(string rutaCertificado, string password);

        /// <summary>
        /// Valida que el certificado sea válido y esté vigente
        /// </summary>
        /// <param name="certificado">Certificado a validar</param>
        /// <returns>True si es válido, False en caso contrario</returns>
        bool ValidarCertificado(X509Certificate2 certificado);

        /// <summary>
        /// Obtiene información detallada del certificado
        /// </summary>
        /// <param name="certificado">Certificado del que obtener información</param>
        /// <returns>Información del certificado</returns>
        InformacionCertificado ObtenerInformacionCertificado(X509Certificate2 certificado);

        /// <summary>
        /// Verifica si el certificado tiene clave privada
        /// </summary>
        /// <param name="certificado">Certificado a verificar</param>
        /// <returns>True si tiene clave privada</returns>
        bool TieneClavePrivada(X509Certificate2 certificado);

        /// <summary>
        /// Verifica si el certificado está expirado
        /// </summary>
        /// <param name="certificado">Certificado a verificar</param>
        /// <returns>True si está expirado</returns>
        bool EstaExpirado(X509Certificate2 certificado);

        /// <summary>
        /// Obtiene el certificado actualmente cargado (singleton)
        /// </summary>
        X509Certificate2 ObtenerCertificadoActual();

        /// <summary>
        /// T-054: Validación completa y estricta del certificado
        /// </summary>
        /// <param name="certificado">Certificado a validar</param>
        /// <returns>Tupla con resultado, errores y advertencias</returns>
        (bool EsValido, List<string> Errores, List<string> Advertencias) ValidarCertificadoCompleto(X509Certificate2 certificado);

        /// <summary>
        /// T-054: Valida y registra en logs el resultado
        /// </summary>
        /// <param name="certificado">Certificado a validar</param>
        /// <returns>True si es válido</returns>
        bool ValidarYRegistrarCertificado(X509Certificate2 certificado);

        /// <summary>
        /// Limpia la caché en memoria para forzar recarga desde BD o archivo.
        /// </summary>
        void RefrescarCertificado();
    }

    /// <summary>
    /// Información detallada de un certificado
    /// </summary>
    public class InformacionCertificado
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Thumbprint { get; set; } = string.Empty;
        public DateTime ValidoDesde { get; set; }
        public DateTime ValidoHasta { get; set; }
        public bool TieneClavePrivada { get; set; }
        public bool EsValido { get; set; }
        public int DiasRestantes { get; set; }
        public string SignatureAlgorithm { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<string> KeyUsages { get; set; } = new();
        public List<string> ExtendedKeyUsages { get; set; } = new();
    }
}