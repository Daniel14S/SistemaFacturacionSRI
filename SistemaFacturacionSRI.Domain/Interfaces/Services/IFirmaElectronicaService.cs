using System.Security.Cryptography.X509Certificates;

namespace SistemaFacturacionSRI.Domain.Interfaces.Services
{
    /// <summary>
    /// Servicio para firma electrónica XADES-BES de documentos XML
    /// T-055: Sprint 3 - Día 6
    /// Implementa el estándar XADES-BES según normativa del SRI Ecuador
    /// </summary>
    public interface IFirmaElectronicaService
    {
        /// <summary>
        /// Firma un documento XML con el certificado digital configurado
        /// </summary>
        /// <param name="xmlSinFirmar">Contenido XML sin firmar</param>
        /// <returns>XML firmado con la estructura XADES-BES</returns>
        Task<string> FirmarXml(string xmlSinFirmar);

        /// <summary>
        /// Firma un documento XML con un certificado específico
        /// </summary>
        /// <param name="xmlSinFirmar">Contenido XML sin firmar</param>
        /// <param name="certificado">Certificado digital a usar para firmar</param>
        /// <returns>XML firmado con la estructura XADES-BES</returns>
        Task<string> FirmarXml(string xmlSinFirmar, X509Certificate2 certificado);

        /// <summary>
        /// Valida la firma de un documento XML
        /// </summary>
        /// <param name="xmlFirmado">XML con firma XADES-BES</param>
        /// <returns>True si la firma es válida, False en caso contrario</returns>
        Task<bool> ValidarFirma(string xmlFirmado);

        /// <summary>
        /// Valida la firma y proporciona información detallada del resultado
        /// </summary>
        /// <param name="xmlFirmado">XML con firma XADES-BES</param>
        /// <returns>Resultado detallado de la validación</returns>
        Task<ResultadoValidacionFirma> ValidarFirmaDetallada(string xmlFirmado);

        /// <summary>
        /// Obtiene información del certificado usado para firmar un XML
        /// </summary>
        /// <param name="xmlFirmado">XML con firma</param>
        /// <returns>Información del certificado</returns>
        Task<InformacionCertificadoFirma> ObtenerInformacionCertificadoFirma(string xmlFirmado);

        /// <summary>
        /// Verifica si un XML tiene firma digital
        /// </summary>
        /// <param name="xml">Contenido XML a verificar</param>
        /// <returns>True si tiene firma, False si no</returns>
        bool TieneFirma(string xml);

        /// <summary>
        /// Extrae el XML original (sin firma) de un XML firmado
        /// </summary>
        /// <param name="xmlFirmado">XML con firma</param>
        /// <returns>XML original sin la firma</returns>
        string ExtraerXmlOriginal(string xmlFirmado);

        /// <summary>
        /// Obtiene el certificado actualmente configurado para firmar
        /// </summary>
        /// <returns>Certificado X509 configurado</returns>
        X509Certificate2 ObtenerCertificadoConfiguracion();
    }

    /// <summary>
    /// Resultado detallado de la validación de una firma
    /// </summary>
    public class ResultadoValidacionFirma
    {
        /// <summary>
        /// Indica si la firma es válida
        /// </summary>
        public bool EsValida { get; set; }

        /// <summary>
        /// Firma verificada correctamente
        /// </summary>
        public bool FirmaVerificada { get; set; }

        /// <summary>
        /// Certificado es válido y vigente
        /// </summary>
        public bool CertificadoValido { get; set; }

        /// <summary>
        /// Hash del documento coincide
        /// </summary>
        public bool HashCorrecto { get; set; }

        /// <summary>
        /// Estructura XADES-BES es correcta
        /// </summary>
        public bool EstructuraCorrecta { get; set; }

        /// <summary>
        /// Fecha y hora de la firma
        /// </summary>
        public DateTime? FechaFirma { get; set; }

        /// <summary>
        /// Subject del certificado usado
        /// </summary>
        public string? SubjectCertificado { get; set; }

        /// <summary>
        /// Lista de errores encontrados
        /// </summary>
        public List<string> Errores { get; set; } = new List<string>();

        /// <summary>
        /// Lista de advertencias
        /// </summary>
        public List<string> Advertencias { get; set; } = new List<string>();

        /// <summary>
        /// Mensaje descriptivo del resultado
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static ResultadoValidacionFirma Exitoso(DateTime fechaFirma, string subject)
        {
            return new ResultadoValidacionFirma
            {
                EsValida = true,
                FirmaVerificada = true,
                CertificadoValido = true,
                HashCorrecto = true,
                EstructuraCorrecta = true,
                FechaFirma = fechaFirma,
                SubjectCertificado = subject,
                Mensaje = "Firma válida y verificada correctamente"
            };
        }

        /// <summary>
        /// Crea un resultado con error
        /// </summary>
        public static ResultadoValidacionFirma ConError(string error)
        {
            return new ResultadoValidacionFirma
            {
                EsValida = false,
                FirmaVerificada = false,
                CertificadoValido = false,
                HashCorrecto = false,
                EstructuraCorrecta = false,
                Errores = new List<string> { error },
                Mensaje = "La firma no es válida"
            };
        }
    }

    /// <summary>
    /// Información del certificado extraída de un XML firmado
    /// </summary>
    public class InformacionCertificadoFirma
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime ValidoDesde { get; set; }
        public DateTime ValidoHasta { get; set; }
        public DateTime? FechaFirma { get; set; }
        public bool EstaVigente { get; set; }
        public string AlgoritmoFirma { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
    }
}