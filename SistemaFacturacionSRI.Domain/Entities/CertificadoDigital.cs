using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.Entities
{
    /// <summary>
    /// Almacena el certificado digital (.p12) y su clave en forma encriptada dentro de la base de datos.
    /// </summary>
    public class CertificadoDigital : EntidadBase
    {
        /// <summary>
        /// Nombre original del archivo cargado por el usuario.
        /// </summary>
        [MaxLength(260)]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Contenido del archivo .p12 encriptado.
        /// </summary>
        public byte[] ArchivoEncriptado { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Clave del certificado encriptada.
        /// </summary>
        public byte[] ClaveEncriptada { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Hash SHA256 del archivo para validar integridad.
        /// </summary>
        [MaxLength(64)]
        public string HashSha256 { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del archivo original en bytes (sin encriptar).
        /// </summary>
        public long TamanoBytes { get; set; }

        /// <summary>
        /// Ambiente del certificado (PRUEBAS o PRODUCCION).
        /// </summary>
        [MaxLength(20)]
        public string Tipo { get; set; } = "PRUEBAS";

        /// <summary>
        /// Fecha de vencimiento detectada al cargar el certificado (si se pudo leer).
        /// </summary>
        public DateTime? FechaExpiracion { get; set; }

        /// <summary>
        /// Indica si este registro es el certificado activo.
        /// </summary>
        public bool EsActivo { get; set; } = true;

        /// <summary>
        /// Texto opcional para notas o comentarios.
        /// </summary>
        [MaxLength(500)]
        public string? Notas { get; set; }
    }
}
