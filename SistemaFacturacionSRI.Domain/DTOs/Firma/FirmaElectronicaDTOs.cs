namespace SistemaFacturacionSRI.Domain.DTOs.Firma
{
    /// <summary>
    /// Opciones para la firma electrónica
    /// </summary>
    public class OpcionesFirma
    {
        /// <summary>
        /// Incluir la cadena completa de certificados
        /// </summary>
        public bool IncluirCadenaCertificados { get; set; } = false;

        /// <summary>
        /// Incluir timestamp (marca de tiempo)
        /// </summary>
        public bool IncluirTimestamp { get; set; } = false;

        /// <summary>
        /// Algoritmo de hash a utilizar
        /// </summary>
        public AlgoritmoHash AlgoritmoHash { get; set; } = AlgoritmoHash.SHA256;

        /// <summary>
        /// Formato de canonicalización
        /// </summary>
        public string CanonicalizationMethod { get; set; } = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

        /// <summary>
        /// ID del nodo a firmar (por defecto "comprobante")
        /// </summary>
        public string IdNodoFirmar { get; set; } = "comprobante";
    }

    /// <summary>
    /// Algoritmos de hash soportados
    /// </summary>
    public enum AlgoritmoHash
    {
        /// <summary>
        /// SHA-1 (requerido por SRI, aunque deprecado)
        /// </summary>
        SHA1,

        /// <summary>
        /// SHA-256 (más seguro, pero verificar compatibilidad SRI)
        /// </summary>
        SHA256,

        /// <summary>
        /// SHA-512
        /// </summary>
        SHA512
    }

    /// <summary>
    /// Resultado de una operación de firma
    /// </summary>
    public class ResultadoFirma
    {
        /// <summary>
        /// Indica si la firma fue exitosa
        /// </summary>
        public bool Exitoso { get; set; }

        /// <summary>
        /// XML firmado (si fue exitoso)
        /// </summary>
        public string? XmlFirmado { get; set; }

        /// <summary>
        /// Mensaje de error (si falló)
        /// </summary>
        public string? MensajeError { get; set; }

        /// <summary>
        /// Detalles adicionales
        /// </summary>
        public string? Detalles { get; set; }

        /// <summary>
        /// Tiempo que tomó la operación
        /// </summary>
        public TimeSpan TiempoTranscurrido { get; set; }

        /// <summary>
        /// Información del certificado usado
        /// </summary>
        public string? CertificadoUsado { get; set; }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static ResultadoFirma CrearExitoso(string xmlFirmado, TimeSpan tiempo, string certificado)
        {
            return new ResultadoFirma
            {
                Exitoso = true,
                XmlFirmado = xmlFirmado,
                TiempoTranscurrido = tiempo,
                CertificadoUsado = certificado,
                Detalles = "Firma realizada correctamente"
            };
        }

        /// <summary>
        /// Crea un resultado con error
        /// </summary>
        public static ResultadoFirma ConError(string mensaje, Exception? ex = null)
        {
            return new ResultadoFirma
            {
                Exitoso = false,
                MensajeError = mensaje,
                Detalles = ex?.Message
            };
        }
    }

    /// <summary>
    /// Estadísticas de firma electrónica
    /// </summary>
    public class EstadisticasFirma
    {
        public int TotalFirmasRealizadas { get; set; }
        public int FirmasExitosas { get; set; }
        public int FirmasFallidas { get; set; }
        public TimeSpan TiempoPromedioFirma { get; set; }
        public DateTime? UltimaFirma { get; set; }
        public string? UltimoCertificadoUsado { get; set; }
    }
}