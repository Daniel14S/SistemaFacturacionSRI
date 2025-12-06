// SistemaFacturacionSRI.Domain/DTOs/SRI/SoapResponseDTOs.cs
// T-073: DTOs para responses SOAP del SRI
// ✅ ACTUALIZADO: Agregado campo Estado en ComprobanteRecibido

namespace SistemaFacturacionSRI.Domain.DTOs.SRI
{
    /// <summary>
    /// T-073: Response de Recepción de Comprobante
    /// </summary>
    public class RespuestaRecepcionComprobante
    {
        /// <summary>
        /// Estado de la recepción: RECIBIDA o DEVUELTA
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Lista de comprobantes recibidos (normalmente 1)
        /// </summary>
        public List<ComprobanteRecibido> Comprobantes { get; set; } = new List<ComprobanteRecibido>();

        /// <summary>
        /// Indica si el comprobante fue recibido exitosamente
        /// </summary>
        public bool FueRecibido => Estado.Equals("RECIBIDA", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Indica si el comprobante fue devuelto (rechazado)
        /// </summary>
        public bool FueDevuelto => Estado.Equals("DEVUELTA", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Obtiene todos los mensajes de error
        /// </summary>
        public List<MensajeSri> ObtenerErrores()
        {
            return Comprobantes
                .SelectMany(c => c.Mensajes)
                .Where(m => m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Obtiene todos los mensajes informativos
        /// </summary>
        public List<MensajeSri> ObtenerInformativos()
        {
            return Comprobantes
                .SelectMany(c => c.Mensajes)
                .Where(m => m.Tipo.Equals("INFORMATIVO", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Obtiene todos los mensajes de advertencia
        /// </summary>
        public List<MensajeSri> ObtenerAdvertencias()
        {
            return Comprobantes
                .SelectMany(c => c.Mensajes)
                .Where(m => m.Tipo.Equals("ADVERTENCIA", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Obtiene un resumen textual de la respuesta
        /// </summary>
        public string ObtenerResumen()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Estado: {Estado}");
            
            if (Comprobantes.Any())
            {
                var comprobante = Comprobantes.First();
                sb.AppendLine($"Clave Acceso: {comprobante.ClaveAcceso}");
                
                var errores = ObtenerErrores();
                if (errores.Any())
                {
                    sb.AppendLine($"Errores ({errores.Count}):");
                    foreach (var error in errores)
                    {
                        sb.AppendLine($"  - [{error.Identificador}] {error.Mensaje}");
                    }
                }
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// T-073: Información de un comprobante recibido
    /// ✅ ACTUALIZADO: Agregado campo Estado
    /// </summary>
    public class ComprobanteRecibido
    {
        /// <summary>
        /// Clave de acceso del comprobante (49 dígitos)
        /// </summary>
        public string ClaveAcceso { get; set; } = string.Empty;

        /// <summary>
        /// ✅ NUEVO: Estado del comprobante individual (si el SRI lo incluye)
        /// Valores posibles: RECIBIDA, DEVUELTA
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Mensajes asociados al comprobante
        /// </summary>
        public List<MensajeSri> Mensajes { get; set; } = new List<MensajeSri>();

        /// <summary>
        /// Indica si tiene mensajes de error
        /// </summary>
        public bool TieneErrores => Mensajes.Any(m => 
            m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
            m.Identificador.StartsWith("4")); // Códigos 4x son errores

        /// <summary>
        /// Obtiene el primer mensaje de error (si existe)
        /// </summary>
        public MensajeSri? PrimerError => Mensajes
            .FirstOrDefault(m => m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// ✅ NUEVO: Obtiene todos los mensajes de error
        /// </summary>
        public List<MensajeSri> ObtenerErrores()
        {
            return Mensajes
                .Where(m => m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                           m.Identificador.StartsWith("4"))
                .ToList();
        }

        /// <summary>
        /// ✅ NUEVO: Obtiene resumen de mensajes
        /// </summary>
        public string ObtenerResumenMensajes()
        {
            if (!Mensajes.Any())
                return "Sin mensajes";

            var errores = Mensajes.Count(m => m.EsError || m.Identificador.StartsWith("4"));
            var advertencias = Mensajes.Count(m => m.EsAdvertencia || m.Identificador.StartsWith("3"));
            var informativos = Mensajes.Count - errores - advertencias;

            return $"Errores: {errores}, Advertencias: {advertencias}, Informativos: {informativos}";
        }
    }

    /// <summary>
    /// T-073: Response de Autorización de Comprobante
    /// </summary>
    public class RespuestaAutorizacionComprobante
    {
        /// <summary>
        /// Clave de acceso consultada
        /// </summary>
        public string ClaveAccesoConsultada { get; set; } = string.Empty;

        /// <summary>
        /// Número de comprobantes en la respuesta
        /// </summary>
        public int NumeroComprobantes { get; set; }

        /// <summary>
        /// Lista de autorizaciones
        /// </summary>
        public List<Autorizacion> Autorizaciones { get; set; } = new List<Autorizacion>();

        /// <summary>
        /// Indica si hay al menos una autorización
        /// </summary>
        public bool TieneAutorizaciones => Autorizaciones.Any();

        /// <summary>
        /// Obtiene la primera autorización (caso más común)
        /// </summary>
        public Autorizacion? PrimeraAutorizacion => Autorizaciones.FirstOrDefault();

        /// <summary>
        /// Indica si el comprobante fue autorizado
        /// </summary>
        public bool FueAutorizado => PrimeraAutorizacion?.EstaAutorizado ?? false;

        /// <summary>
        /// Indica si el comprobante fue rechazado
        /// </summary>
        public bool FueRechazado => PrimeraAutorizacion?.EstaNoAutorizado ?? false;

        /// <summary>
        /// Indica si el comprobante está en procesamiento
        /// </summary>
        public bool EnProcesamiento => PrimeraAutorizacion?.EnProcesamiento ?? false;
    }

    /// <summary>
    /// T-073: Información de una autorización
    /// </summary>
    public class Autorizacion
    {
        /// <summary>
        /// Estado: AUTORIZADO, NO AUTORIZADO, EN PROCESAMIENTO
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Número de autorización (10 dígitos)
        /// </summary>
        public string NumeroAutorizacion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de autorización
        /// Formato: dd/MM/yyyy HH:mm:ss
        /// </summary>
        public string FechaAutorizacion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de autorización parseada
        /// </summary>
        public DateTime? FechaAutorizacionParsed { get; set; }

        /// <summary>
        /// Ambiente: PRUEBAS o PRODUCCION
        /// </summary>
        public string Ambiente { get; set; } = string.Empty;

        /// <summary>
        /// XML del comprobante autorizado (CDATA)
        /// </summary>
        public string Comprobante { get; set; } = string.Empty;

        /// <summary>
        /// Mensajes asociados a la autorización
        /// </summary>
        public List<MensajeSri> Mensajes { get; set; } = new List<MensajeSri>();

        /// <summary>
        /// Clave de acceso (si viene en la autorización)
        /// </summary>
        public string? ClaveAcceso { get; set; }

        // Estados predefinidos
        public bool EstaAutorizado => Estado.Equals("AUTORIZADO", StringComparison.OrdinalIgnoreCase);
        public bool EstaNoAutorizado => Estado.Equals("NO AUTORIZADO", StringComparison.OrdinalIgnoreCase);
        public bool EnProcesamiento => Estado.Equals("EN PROCESAMIENTO", StringComparison.OrdinalIgnoreCase);
        public bool FueDevuelta => Estado.Equals("DEVUELTA", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Indica si tiene mensajes de error
        /// </summary>
        public bool TieneErrores => Mensajes.Any(m => 
            m.Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Parsea la fecha de autorización desde el formato SRI
        /// </summary>
        public void ParsearFechaAutorizacion()
        {
            if (string.IsNullOrWhiteSpace(FechaAutorizacion))
            {
                FechaAutorizacionParsed = null;
                return;
            }

            // Formato SRI: dd/MM/yyyy HH:mm:ss
            if (DateTime.TryParseExact(
                FechaAutorizacion,
                "dd/MM/yyyy HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var fecha))
            {
                FechaAutorizacionParsed = fecha;
            }
            else if (DateTime.TryParse(FechaAutorizacion, out fecha))
            {
                FechaAutorizacionParsed = fecha;
            }
        }

        /// <summary>
        /// Obtiene resumen de la autorización
        /// </summary>
        public string ObtenerResumen()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Estado: {Estado}");
            
            if (!string.IsNullOrEmpty(NumeroAutorizacion))
            {
                sb.AppendLine($"Número Autorización: {NumeroAutorizacion}");
            }
            
            if (!string.IsNullOrEmpty(FechaAutorizacion))
            {
                sb.AppendLine($"Fecha: {FechaAutorizacion}");
            }
            
            if (Mensajes.Any())
            {
                sb.AppendLine($"Mensajes ({Mensajes.Count}):");
                foreach (var mensaje in Mensajes)
                {
                    sb.AppendLine($"  [{mensaje.Tipo}] {mensaje.Mensaje}");
                }
            }
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// T-073: Mensaje del SRI (error, advertencia, informativo)
    /// </summary>
    public class MensajeSri
    {
        /// <summary>
        /// Identificador numérico del mensaje
        /// </summary>
        public string Identificador { get; set; } = string.Empty;

        /// <summary>
        /// Texto del mensaje
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Información adicional del mensaje
        /// </summary>
        public string? InformacionAdicional { get; set; }

        /// <summary>
        /// Tipo: ERROR, ADVERTENCIA, INFORMATIVO
        /// </summary>
        public string Tipo { get; set; } = string.Empty;

        // Propiedades de ayuda
        public bool EsError => Tipo.Equals("ERROR", StringComparison.OrdinalIgnoreCase);
        public bool EsAdvertencia => Tipo.Equals("ADVERTENCIA", StringComparison.OrdinalIgnoreCase);
        public bool EsInformativo => Tipo.Equals("INFORMATIVO", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Obtiene el identificador como entero
        /// </summary>
        public int? IdentificadorNumerico
        {
            get
            {
                if (int.TryParse(Identificador, out var numero))
                {
                    return numero;
                }
                return null;
            }
        }

        /// <summary>
        /// Representación en string del mensaje
        /// </summary>
        public override string ToString()
        {
            var msg = $"[{Tipo}] [{Identificador}] {Mensaje}";
            if (!string.IsNullOrWhiteSpace(InformacionAdicional))
            {
                msg += $" - {InformacionAdicional}";
            }
            return msg;
        }
    }

    /// <summary>
    /// T-073: Códigos de mensaje comunes del SRI
    /// </summary>
    public static class CodigosMensajeSri
    {
        // Informativos
        public const string CLAVE_ACCESO_REGISTRADA = "43";
        public const string COMPROBANTE_AUTORIZADO = "60";

        // Errores comunes
        public const string CLAVE_ACCESO_INCORRECTA = "65";
        public const string CERTIFICADO_INVALIDO = "69";
        public const string FIRMA_ELECTRONICA_NO_VALIDA = "70";
        public const string SECUENCIAL_DUPLICADO = "235";
        public const string ERROR_INTERNO_SISTEMA = "999";

        // Advertencias
        public const string CERTIFICADO_POR_EXPIRAR = "68";

        /// <summary>
        /// Determina si un código indica éxito
        /// </summary>
        public static bool EsCodigoExitoso(string codigo)
        {
            return codigo == CLAVE_ACCESO_REGISTRADA || 
                   codigo == COMPROBANTE_AUTORIZADO;
        }

        /// <summary>
        /// Determina si un código indica error crítico
        /// </summary>
        public static bool EsErrorCritico(string codigo)
        {
            return codigo == CERTIFICADO_INVALIDO ||
                   codigo == FIRMA_ELECTRONICA_NO_VALIDA ||
                   codigo == SECUENCIAL_DUPLICADO;
        }

        /// <summary>
        /// Obtiene descripción del código
        /// </summary>
        public static string ObtenerDescripcion(string codigo)
        {
            return codigo switch
            {
                CLAVE_ACCESO_REGISTRADA => "Clave de acceso registrada correctamente",
                COMPROBANTE_AUTORIZADO => "Comprobante autorizado",
                CLAVE_ACCESO_INCORRECTA => "Clave de acceso incorrecta",
                CERTIFICADO_INVALIDO => "Certificado digital inválido",
                FIRMA_ELECTRONICA_NO_VALIDA => "Firma electrónica no válida",
                SECUENCIAL_DUPLICADO => "Número secuencial duplicado",
                CERTIFICADO_POR_EXPIRAR => "Certificado próximo a expirar",
                ERROR_INTERNO_SISTEMA => "Error interno del sistema SRI",
                _ => "Código desconocido"
            };
        }
    }

    /// <summary>
    /// T-073: Estados de comprobante según el SRI
    /// </summary>
    public static class EstadosComprobanteSri
    {
        // Estados de Recepción
        public const string RECIBIDA = "RECIBIDA";
        public const string DEVUELTA = "DEVUELTA";

        // Estados de Autorización
        public const string AUTORIZADO = "AUTORIZADO";
        public const string NO_AUTORIZADO = "NO AUTORIZADO";
        public const string EN_PROCESAMIENTO = "EN PROCESAMIENTO";

        /// <summary>
        /// Verifica si un estado indica éxito
        /// </summary>
        public static bool EsEstadoExitoso(string estado)
        {
            return estado.Equals(RECIBIDA, StringComparison.OrdinalIgnoreCase) ||
                   estado.Equals(AUTORIZADO, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si un estado indica fallo
        /// </summary>
        public static bool EsEstadoFallo(string estado)
        {
            return estado.Equals(DEVUELTA, StringComparison.OrdinalIgnoreCase) ||
                   estado.Equals(NO_AUTORIZADO, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si un estado indica que debe reintentarse
        /// </summary>
        public static bool DebeReintentar(string estado)
        {
            return estado.Equals(EN_PROCESAMIENTO, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// T-073: Resultado genérico de operación con el SRI
    /// </summary>
    public class ResultadoOperacionSri
    {
        /// <summary>
        /// Indica si la operación fue exitosa
        /// </summary>
        public bool Exitoso { get; set; }

        /// <summary>
        /// Clave de acceso del comprobante
        /// </summary>
        public string ClaveAcceso { get; set; } = string.Empty;

        /// <summary>
        /// Estado final del comprobante
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Número de autorización (si fue autorizado)
        /// </summary>
        public string? NumeroAutorizacion { get; set; }

        /// <summary>
        /// Fecha de autorización (si fue autorizado)
        /// </summary>
        public DateTime? FechaAutorizacion { get; set; }

        /// <summary>
        /// Mensajes del SRI
        /// </summary>
        public List<MensajeSri> Mensajes { get; set; } = new List<MensajeSri>();

        /// <summary>
        /// XML del comprobante autorizado
        /// </summary>
        public string? XmlAutorizado { get; set; }

        /// <summary>
        /// Número de intentos realizados
        /// </summary>
        public int NumeroIntentos { get; set; } = 1;

        /// <summary>
        /// Tiempo total transcurrido
        /// </summary>
        public TimeSpan TiempoTranscurrido { get; set; }

        /// <summary>
        /// Mensaje de error (si falló)
        /// </summary>
        public string? MensajeError { get; set; }

        /// <summary>
        /// Detalles adicionales
        /// </summary>
        public Dictionary<string, object> DatosAdicionales { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static ResultadoOperacionSri CrearExitoso(
            string claveAcceso,
            string numeroAutorizacion,
            DateTime fechaAutorizacion,
            string xmlAutorizado)
        {
            return new ResultadoOperacionSri
            {
                Exitoso = true,
                ClaveAcceso = claveAcceso,
                Estado = EstadosComprobanteSri.AUTORIZADO,
                NumeroAutorizacion = numeroAutorizacion,
                FechaAutorizacion = fechaAutorizacion,
                XmlAutorizado = xmlAutorizado
            };
        }

        /// <summary>
        /// Crea un resultado fallido
        /// </summary>
        public static ResultadoOperacionSri Fallido(
            string claveAcceso,
            string estado,
            string mensajeError,
            List<MensajeSri>? mensajes = null)
        {
            return new ResultadoOperacionSri
            {
                Exitoso = false,
                ClaveAcceso = claveAcceso,
                Estado = estado,
                MensajeError = mensajeError,
                Mensajes = mensajes ?? new List<MensajeSri>()
            };
        }

        /// <summary>
        /// Obtiene resumen textual del resultado
        /// </summary>
        public string ObtenerResumen()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Estado: {(Exitoso ? "✅ Exitoso" : "❌ Fallido")}");
            sb.AppendLine($"Clave Acceso: {ClaveAcceso}");
            sb.AppendLine($"Estado SRI: {Estado}");
            
            if (!string.IsNullOrEmpty(NumeroAutorizacion))
            {
                sb.AppendLine($"Número Autorización: {NumeroAutorizacion}");
            }
            
            if (FechaAutorizacion.HasValue)
            {
                sb.AppendLine($"Fecha Autorización: {FechaAutorizacion:dd/MM/yyyy HH:mm:ss}");
            }
            
            if (!string.IsNullOrEmpty(MensajeError))
            {
                sb.AppendLine($"Error: {MensajeError}");
            }
            
            if (Mensajes.Any())
            {
                sb.AppendLine($"\nMensajes ({Mensajes.Count}):");
                foreach (var mensaje in Mensajes)
                {
                    sb.AppendLine($"  {mensaje}");
                }
            }
            
            sb.AppendLine($"\nIntentos: {NumeroIntentos}");
            sb.AppendLine($"Tiempo: {TiempoTranscurrido.TotalSeconds:F2}s");
            
            return sb.ToString();
        }
    }
}