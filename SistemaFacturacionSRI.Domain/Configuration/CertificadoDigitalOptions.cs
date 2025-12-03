namespace SistemaFacturacionSRI.Domain.Configuration
{
    /// <summary>
    /// Configuración del certificado digital para firma electrónica
    /// T-052: Sprint 3 - Día 6
    /// </summary>
    public class CertificadoDigitalOptions
    {
        /// <summary>
        /// Sección en appsettings.json
        /// </summary>
        public const string SectionName = "CertificadoDigital";

        /// <summary>
        /// Ruta al archivo del certificado (.p12 o .pfx)
        /// Puede ser ruta relativa o absoluta
        /// </summary>
        public string RutaCertificado { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del certificado
        /// IMPORTANTE: En producción usar variables de entorno o Azure Key Vault
        /// </summary>
        public string ClaveCertificado { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de certificado: PRUEBAS o PRODUCCION
        /// </summary>
        public string TipoCertificado { get; set; } = "PRUEBAS";

        /// <summary>
        /// Emisor del certificado (referencia)
        /// </summary>
        public string? Emisor { get; set; }

        /// <summary>
        /// Fecha desde cuando es válido (referencia)
        /// </summary>
        public string? ValidoDesde { get; set; }

        /// <summary>
        /// Fecha hasta cuando es válido (referencia)
        /// </summary>
        public string? ValidoHasta { get; set; }

        /// <summary>
        /// Indica si se debe validar la vigencia del certificado
        /// </summary>
        public bool ValidarVigencia { get; set; } = true;

        /// <summary>
        /// Indica si se debe validar la cadena de confianza
        /// </summary>
        public bool ValidarCadenaConfianza { get; set; } = false;

        /// <summary>
        /// Obtiene la ruta absoluta del certificado
        /// </summary>
        public string ObtenerRutaAbsoluta()
        {
            // Si es ruta absoluta, retornarla directamente
            if (Path.IsPathRooted(RutaCertificado))
            {
                return RutaCertificado;
            }

            // Si es ruta relativa, combinarla con el directorio base de la aplicación
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, RutaCertificado);
        }

        /// <summary>
        /// Valida que la configuración sea correcta
        /// </summary>
        public void Validar()
        {
            if (string.IsNullOrWhiteSpace(RutaCertificado))
            {
                throw new InvalidOperationException(
                    "La ruta del certificado no está configurada. " +
                    "Configure 'CertificadoDigital:RutaCertificado' en appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(ClaveCertificado))
            {
                throw new InvalidOperationException(
                    "La contraseña del certificado no está configurada. " +
                    "Configure 'CertificadoDigital:ClaveCertificado' en appsettings.json o como variable de entorno");
            }

            var rutaAbsoluta = ObtenerRutaAbsoluta();
            if (!File.Exists(rutaAbsoluta))
            {
                throw new FileNotFoundException(
                    $"No se encontró el archivo del certificado en: {rutaAbsoluta}. " +
                    $"Verifique la configuración 'CertificadoDigital:RutaCertificado'");
            }
        }
    }
}