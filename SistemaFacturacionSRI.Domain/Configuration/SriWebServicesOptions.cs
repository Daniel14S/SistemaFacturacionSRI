// SistemaFacturacionSRI.Domain/Configuration/SriWebServicesOptions.cs
// T-070: Configuración de URLs y opciones de WebServices del SRI
// ✅ CORREGIDO: Dominios correctos del SRI

using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionSRI.Domain.Configuration
{
    /// <summary>
    /// T-070: Opciones de configuración para WebServices SOAP del SRI
    /// </summary>
    public class SriWebServicesOptions
    {
        public const string SectionName = "SRI";

        /// <summary>
        /// Ambiente actual: PRUEBAS o PRODUCCION
        /// </summary>
        [Required(ErrorMessage = "El ambiente es requerido")]
        public string Ambiente { get; set; } = "PRUEBAS";

        /// <summary>
        /// Código de ambiente: 1 = PRUEBAS, 2 = PRODUCCION
        /// </summary>
        [Range(1, 2, ErrorMessage = "Código de ambiente debe ser 1 (Pruebas) o 2 (Producción)")]
        public int AmbienteCodigo { get; set; } = 1;

        /// <summary>
        /// URL del WebService de Recepción de Comprobantes
        /// </summary>
        [Required(ErrorMessage = "URL de recepción es requerida")]
        [Url(ErrorMessage = "URL de recepción inválida")]
        public string UrlRecepcion { get; set; } = string.Empty;

        /// <summary>
        /// URL del WebService de Autorización de Comprobantes
        /// </summary>
        [Required(ErrorMessage = "URL de autorización es requerida")]
        [Url(ErrorMessage = "URL de autorización inválida")]
        public string UrlAutorizacion { get; set; } = string.Empty;

        /// <summary>
        /// Timeout en segundos para llamadas HTTP
        /// </summary>
        [Range(5, 120, ErrorMessage = "Timeout debe estar entre 5 y 120 segundos")]
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>
        /// Número máximo de reintentos en caso de error
        /// </summary>
        [Range(0, 10, ErrorMessage = "Reintentos debe estar entre 0 y 10")]
        public int ReintentoMaximo { get; set; } = 3;

        /// <summary>
        /// Delay en milisegundos entre reintentos
        /// </summary>
        [Range(500, 30000, ErrorMessage = "Delay debe estar entre 500ms y 30000ms")]
        public int DelayEntreReintentosMs { get; set; } = 2000;

        /// <summary>
        /// Usar delay exponencial en reintentos (2s, 4s, 8s, ...)
        /// </summary>
        public bool UsarDelayExponencial { get; set; } = true;

        /// <summary>
        /// Segundos de espera antes de consultar autorización después de recepción
        /// </summary>
        [Range(1, 60, ErrorMessage = "Espera debe estar entre 1 y 60 segundos")]
        public int EsperaAntesConsultaSegundos { get; set; } = 5;

        /// <summary>
        /// Número máximo de intentos para consultar autorización
        /// cuando está EN PROCESAMIENTO
        /// </summary>
        [Range(1, 20, ErrorMessage = "Intentos consulta debe estar entre 1 y 20")]
        public int MaximosIntentosConsulta { get; set; } = 10;

        /// <summary>
        /// Habilitar logs detallados de SOAP
        /// </summary>
        public bool LogSoapDetallado { get; set; } = false;

        /// <summary>
        /// Validar certificado SSL del SRI
        /// </summary>
        public bool ValidarCertificadoSsl { get; set; } = true;

        /// <summary>
        /// User-Agent para las peticiones HTTP
        /// </summary>
        public string UserAgent { get; set; } = "SistemaFacturacionSRI/1.0";

        /// <summary>
        /// Validar que la configuración sea correcta
        /// </summary>
        public void Validar()
        {
            var validationContext = new ValidationContext(this);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

            if (!isValid)
            {
                var errores = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
                throw new InvalidOperationException($"Configuración SRI inválida: {errores}");
            }

            // Validaciones adicionales
            if (Ambiente.ToUpper() != "PRUEBAS" && Ambiente.ToUpper() != "PRODUCCION")
            {
                throw new InvalidOperationException("Ambiente debe ser 'PRUEBAS' o 'PRODUCCION'");
            }

            if ((Ambiente.ToUpper() == "PRUEBAS" && AmbienteCodigo != 1) ||
                (Ambiente.ToUpper() == "PRODUCCION" && AmbienteCodigo != 2))
            {
                throw new InvalidOperationException(
                    $"Ambiente '{Ambiente}' no coincide con AmbienteCodigo {AmbienteCodigo}");
            }

            // ✅ CORREGIDO: Validar URLs según ambiente con dominios correctos
            // MODIFICACIÓN PARA SIMULADOR: Permitir localhost para pruebas
            if (Ambiente.ToUpper() == "PRUEBAS" && !UrlRecepcion.Contains("celcer.sri.gob.ec") && !UrlRecepcion.Contains("localhost"))
            {
                throw new InvalidOperationException(
                    "URL de recepción para PRUEBAS debe contener 'celcer.sri.gob.ec' o 'localhost' (simulador)");
            }

            if (Ambiente.ToUpper() == "PRODUCCION" && UrlRecepcion.Contains("celcer"))
            {
                throw new InvalidOperationException(
                    "URL de recepción para PRODUCCION no debe contener 'celcer' (debe ser 'cel.sri.gob.ec')");
            }
        }

        /// <summary>
        /// Obtiene el SOAPAction para recepción (vacío según especificación SRI)
        /// </summary>
        public string GetSoapActionRecepcion() => "";

        /// <summary>
        /// Obtiene el SOAPAction para autorización (vacío según especificación SRI)
        /// </summary>
        public string GetSoapActionAutorizacion() => "";

        /// <summary>
        /// Calcula el delay para un intento específico
        /// </summary>
        public int CalcularDelay(int numeroIntento)
        {
            if (!UsarDelayExponencial)
            {
                return DelayEntreReintentosMs;
            }

            // Delay exponencial: 2s, 4s, 8s, 16s, ...
            return DelayEntreReintentosMs * (int)Math.Pow(2, numeroIntento - 1);
        }

        /// <summary>
        /// Retorna información del ambiente configurado
        /// </summary>
        public string ObtenerDescripcionAmbiente()
        {
            return $"{Ambiente.ToUpper()} (Código: {AmbienteCodigo})";
        }
    }

    /// <summary>
    /// T-070: Constantes para ambientes SRI
    /// ✅ CORREGIDO: URLs oficiales actualizadas
    /// </summary>
    public static class SriAmbientes
    {
        public const int PRUEBAS = 1;
        public const int PRODUCCION = 2;

        public const string PRUEBAS_NOMBRE = "PRUEBAS";
        public const string PRODUCCION_NOMBRE = "PRODUCCION";

        // ✅ URLs oficiales CORREGIDAS del SRI
        public static class Urls
        {
            // ✅ Ambiente PRUEBAS - Dominio correcto: celcer.sri.gob.ec
            public const string PRUEBAS_RECEPCION = 
                "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
            
            public const string PRUEBAS_AUTORIZACION = 
                "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";

            // ✅ Ambiente PRODUCCION - Dominio: cel.sri.gob.ec
            public const string PRODUCCION_RECEPCION = 
                "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";
            
            public const string PRODUCCION_AUTORIZACION = 
                "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl";
        }

        /// <summary>
        /// Obtiene el nombre del ambiente por código
        /// </summary>
        public static string ObtenerNombreAmbiente(int codigo)
        {
            return codigo switch
            {
                PRUEBAS => PRUEBAS_NOMBRE,
                PRODUCCION => PRODUCCION_NOMBRE,
                _ => throw new ArgumentException($"Código de ambiente inválido: {codigo}")
            };
        }

        /// <summary>
        /// Obtiene el código del ambiente por nombre
        /// </summary>
        public static int ObtenerCodigoAmbiente(string nombre)
        {
            return nombre.ToUpper() switch
            {
                PRUEBAS_NOMBRE => PRUEBAS,
                PRODUCCION_NOMBRE => PRODUCCION,
                _ => throw new ArgumentException($"Nombre de ambiente inválido: {nombre}")
            };
        }

        /// <summary>
        /// Valida si un código de ambiente es válido
        /// </summary>
        public static bool EsAmbienteValido(int codigo)
        {
            return codigo == PRUEBAS || codigo == PRODUCCION;
        }
    }
}