// SistemaFacturacionSRI.Domain/DTOs/SRI/SoapRequestDTOs.cs
// T-072: DTOs para requests SOAP al SRI

namespace SistemaFacturacionSRI.Domain.DTOs.SRI
{
    /// <summary>
    /// T-072: Request para validar comprobante (Recepción)
    /// </summary>
    public class RecepcionComprobanteRequest
    {
        /// <summary>
        /// XML del comprobante firmado (en Base64 o texto con CDATA)
        /// </summary>
        public string XmlComprobante { get; set; } = string.Empty;

        /// <summary>
        /// Clave de acceso del comprobante (49 dígitos)
        /// </summary>
        public string ClaveAcceso { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de comprobante (01=Factura, 04=Nota Crédito, etc.)
        /// </summary>
        public string TipoComprobante { get; set; } = "01";

        /// <summary>
        /// RUC del emisor
        /// </summary>
        public string RucEmisor { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión del comprobante
        /// </summary>
        public DateTime FechaEmision { get; set; }

        /// <summary>
        /// Valida que el request tenga los datos mínimos requeridos
        /// </summary>
        public void Validar()
        {
            if (string.IsNullOrWhiteSpace(XmlComprobante))
            {
                throw new ArgumentException("El XML del comprobante es requerido");
            }

            if (string.IsNullOrWhiteSpace(ClaveAcceso))
            {
                throw new ArgumentException("La clave de acceso es requerida");
            }

            if (ClaveAcceso.Length != 49)
            {
                throw new ArgumentException($"La clave de acceso debe tener 49 dígitos, tiene {ClaveAcceso.Length}");
            }

            if (!ClaveAcceso.All(char.IsDigit))
            {
                throw new ArgumentException("La clave de acceso debe contener solo dígitos");
            }

            if (string.IsNullOrWhiteSpace(RucEmisor))
            {
                throw new ArgumentException("El RUC del emisor es requerido");
            }

            if (RucEmisor.Length != 13)
            {
                throw new ArgumentException($"El RUC debe tener 13 dígitos, tiene {RucEmisor.Length}");
            }
        }

        /// <summary>
        /// Genera el XML SOAP para enviar al SRI
        /// </summary>
        /// <summary>
        /// Genera el XML SOAP para enviar al SRI (Base64 - formato requerido)
        /// </summary>
        public string GenerarSoapXml()
        {
            // ✅ CRÍTICO: El SRI requiere el XML en Base64, NO en CDATA
            string xmlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(XmlComprobante));

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                  xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
    <soapenv:Header/>
    <soapenv:Body>
        <ec:validarComprobante>
            <xml>{xmlBase64}</xml>
        </ec:validarComprobante>
    </soapenv:Body>
</soapenv:Envelope>";
        }
    }

    /// <summary>
    /// T-072: Request para consultar autorización de comprobante
    /// </summary>
    public class AutorizacionComprobanteRequest
    {
        /// <summary>
        /// Clave de acceso del comprobante a consultar (49 dígitos)
        /// </summary>
        public string ClaveAcceso { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de comprobante (opcional, para logging)
        /// </summary>
        public string? TipoComprobante { get; set; }

        /// <summary>
        /// RUC del emisor (opcional, para logging)
        /// </summary>
        public string? RucEmisor { get; set; }

        /// <summary>
        /// Número de intento (para control de reintentos)
        /// </summary>
        public int NumeroIntento { get; set; } = 1;

        /// <summary>
        /// Valida que el request tenga los datos mínimos requeridos
        /// </summary>
        public void Validar()
        {
            if (string.IsNullOrWhiteSpace(ClaveAcceso))
            {
                throw new ArgumentException("La clave de acceso es requerida");
            }

            if (ClaveAcceso.Length != 49)
            {
                throw new ArgumentException($"La clave de acceso debe tener 49 dígitos, tiene {ClaveAcceso.Length}");
            }

            if (!ClaveAcceso.All(char.IsDigit))
            {
                throw new ArgumentException("La clave de acceso debe contener solo dígitos");
            }

            if (NumeroIntento < 1)
            {
                throw new ArgumentException("El número de intento debe ser mayor a 0");
            }
        }

        /// <summary>
        /// Genera el XML SOAP para consultar autorización
        /// </summary>
        /// <summary>
        /// Genera el XML SOAP para consultar autorización
        /// </summary>
        public string GenerarSoapXml()
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                  xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
    <soapenv:Header/>
    <soapenv:Body>
        <ec:autorizacionComprobante>
            <claveAccesoComprobante>{ClaveAcceso}</claveAccesoComprobante>
        </ec:autorizacionComprobante>
    </soapenv:Body>
</soapenv:Envelope>";
        }
    }

    /// <summary>
    /// T-072: Request genérico para operaciones SOAP
    /// </summary>
    public class SoapRequestBase
    {
        /// <summary>
        /// URL del endpoint SOAP
        /// </summary>
        public string EndpointUrl { get; set; } = string.Empty;

        /// <summary>
        /// SOAPAction (normalmente vacío para SRI)
        /// </summary>
        public string SoapAction { get; set; } = "";

        /// <summary>
        /// Timeout en segundos
        /// </summary>
        public int TimeoutSegundos { get; set; } = 30;

        /// <summary>
        /// Headers HTTP adicionales
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// User-Agent para la petición
        /// </summary>
        public string UserAgent { get; set; } = "SistemaFacturacionSRI/1.0";

        /// <summary>
        /// Identificador único de la petición (para trazabilidad)
        /// </summary>
        public Guid RequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp de la petición
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// T-072: Opciones para retry de requests SOAP
    /// </summary>
    public class SoapRetryOptions
    {
        /// <summary>
        /// Número máximo de reintentos
        /// </summary>
        public int MaximoReintentos { get; set; } = 3;

        /// <summary>
        /// Delay inicial en milisegundos
        /// </summary>
        public int DelayInicialMs { get; set; } = 2000;

        /// <summary>
        /// Usar backoff exponencial (2s, 4s, 8s, ...)
        /// </summary>
        public bool BackoffExponencial { get; set; } = true;

        /// <summary>
        /// Factor de multiplicación para backoff
        /// </summary>
        public double FactorBackoff { get; set; } = 2.0;

        /// <summary>
        /// Delay máximo en milisegundos
        /// </summary>
        public int DelayMaximoMs { get; set; } = 30000;

        /// <summary>
        /// Calcula el delay para un intento específico
        /// </summary>
        public int CalcularDelay(int numeroIntento)
        {
            if (!BackoffExponencial)
            {
                return DelayInicialMs;
            }

            var delay = DelayInicialMs * Math.Pow(FactorBackoff, numeroIntento - 1);
            return (int)Math.Min(delay, DelayMaximoMs);
        }
    }

    /// <summary>
    /// T-072: Contexto de ejecución de request SOAP
    /// </summary>
    public class SoapRequestContext
    {
        /// <summary>
        /// ID único del request
        /// </summary>
        public Guid RequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp de inicio
        /// </summary>
        public DateTime Inicio { get; set; } = DateTime.Now;

        /// <summary>
        /// Timestamp de fin
        /// </summary>
        public DateTime? Fin { get; set; }

        /// <summary>
        /// Duración en milisegundos
        /// </summary>
        public long? DuracionMs => Fin.HasValue 
            ? (long)(Fin.Value - Inicio).TotalMilliseconds 
            : null;

        /// <summary>
        /// Número de intento actual
        /// </summary>
        public int NumeroIntento { get; set; } = 1;

        /// <summary>
        /// Operación que se está ejecutando
        /// </summary>
        public string Operacion { get; set; } = string.Empty;

        /// <summary>
        /// URL del endpoint
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del request en bytes
        /// </summary>
        public int? TamanoRequestBytes { get; set; }

        /// <summary>
        /// Tamaño de la respuesta en bytes
        /// </summary>
        public int? TamanoResponseBytes { get; set; }

        /// <summary>
        /// Código de estado HTTP
        /// </summary>
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Indica si fue exitoso
        /// </summary>
        public bool Exitoso { get; set; }

        /// <summary>
        /// Mensaje de error si hubo
        /// </summary>
        public string? MensajeError { get; set; }

        /// <summary>
        /// Marca el fin del request
        /// </summary>
        public void MarcarFin(bool exitoso, string? error = null)
        {
            Fin = DateTime.Now;
            Exitoso = exitoso;
            MensajeError = error;
        }

        /// <summary>
        /// Retorna un resumen del contexto
        /// </summary>
        public override string ToString()
        {
            return $"[{RequestId}] {Operacion} - Intento {NumeroIntento} - " +
                   $"{(Exitoso ? "✅ Exitoso" : "❌ Fallo")} - {DuracionMs}ms";
        }
    }
}