// SistemaFacturacionSRI.Domain/Exceptions/FirmaElectronicaExceptions.cs
// T-057: Excepciones básicas
// T-066: MANEJO COMPLETO DE ERRORES DE FIRMA

namespace SistemaFacturacionSRI.Domain.Exceptions
{
    /// <summary>
    /// Excepción base para errores de firma electrónica
    /// </summary>
    public class FirmaElectronicaException : Exception
    {
        public string CodigoError { get; set; }
        public Dictionary<string, object> DatosAdicionales { get; set; }

        public FirmaElectronicaException(string message, string codigoError = "FIRMA_ERROR") 
            : base(message)
        {
            CodigoError = codigoError;
            DatosAdicionales = new Dictionary<string, object>();
        }

        public FirmaElectronicaException(string message, Exception innerException, string codigoError = "FIRMA_ERROR")
            : base(message, innerException)
        {
            CodigoError = codigoError;
            DatosAdicionales = new Dictionary<string, object>();
        }

        public void AgregarDato(string clave, object valor)
        {
            DatosAdicionales[clave] = valor;
        }
    }

    // ============================================================
    // T-066: EXCEPCIONES ESPECÍFICAS DE CERTIFICADO
    // ============================================================

    /// <summary>
    /// T-057, T-066: Excepción cuando el certificado no es válido
    /// </summary>
    public class CertificadoInvalidoException : FirmaElectronicaException
    {
        public List<string> Errores { get; set; }
        public List<string> Advertencias { get; set; }

        public CertificadoInvalidoException(string mensaje, List<string>? errores = null, List<string>? advertencias = null)
            : base($"Certificado inválido: {mensaje}", "CERT_INVALIDO")
        {
            Errores = errores ?? new List<string>();
            Advertencias = advertencias ?? new List<string>();
            
            if (Errores.Any())
            {
                AgregarDato("errores", string.Join("; ", Errores));
            }
        }

        public CertificadoInvalidoException(string mensaje, Exception innerException)
            : base($"Certificado inválido: {mensaje}", innerException, "CERT_INVALIDO")
        {
            Errores = new List<string>();
            Advertencias = new List<string>();
        }
    }

    /// <summary>
    /// T-057, T-066: Excepción cuando el certificado está expirado
    /// </summary>
    public class CertificadoExpiradoException : FirmaElectronicaException
    {
        public DateTime FechaExpiracion { get; }
        public int DiasExpirados { get; }

        public CertificadoExpiradoException(DateTime fechaExpiracion)
            : base($"El certificado expiró el {fechaExpiracion:dd/MM/yyyy}. No se puede usar para firmar.", "CERT_EXPIRADO")
        {
            FechaExpiracion = fechaExpiracion;
            DiasExpirados = (DateTime.Now - fechaExpiracion).Days;
            
            AgregarDato("fechaExpiracion", fechaExpiracion);
            AgregarDato("diasExpirados", DiasExpirados);
        }
    }

    /// <summary>
    /// T-066: Excepción cuando el certificado está próximo a expirar
    /// </summary>
    public class CertificadoPorExpirarException : FirmaElectronicaException
    {
        public DateTime FechaExpiracion { get; }
        public int DiasRestantes { get; }

        public CertificadoPorExpirarException(DateTime fechaExpiracion, int diasRestantes)
            : base($"⚠️ ADVERTENCIA: El certificado expira en {diasRestantes} días ({fechaExpiracion:dd/MM/yyyy}). " +
                   "Se recomienda renovarlo pronto para evitar interrupciones.", "CERT_POR_EXPIRAR")
        {
            FechaExpiracion = fechaExpiracion;
            DiasRestantes = diasRestantes;
            
            AgregarDato("fechaExpiracion", fechaExpiracion);
            AgregarDato("diasRestantes", diasRestantes);
        }
    }

    /// <summary>
    /// T-057, T-066: Excepción cuando falta la clave privada
    /// </summary>
    public class ClavePrivadaNoDisponibleException : FirmaElectronicaException
    {
        public ClavePrivadaNoDisponibleException()
            : base("El certificado no contiene clave privada. Se requiere para firmar documentos.", "CERT_SIN_CLAVE")
        {
        }

        public ClavePrivadaNoDisponibleException(string detalles)
            : base($"El certificado no contiene clave privada accesible. {detalles}", "CERT_SIN_CLAVE")
        {
        }
    }

    /// <summary>
    /// T-066: Excepción cuando el certificado no puede cargarse
    /// </summary>
    public class CertificadoNoCargadoException : FirmaElectronicaException
    {
        public string RutaCertificado { get; }

        public CertificadoNoCargadoException(string rutaCertificado, string motivo)
            : base($"No se pudo cargar el certificado desde '{rutaCertificado}': {motivo}", "CERT_NO_CARGADO")
        {
            RutaCertificado = rutaCertificado;
            AgregarDato("rutaCertificado", rutaCertificado);
        }

        public CertificadoNoCargadoException(string rutaCertificado, Exception innerException)
            : base($"No se pudo cargar el certificado desde '{rutaCertificado}'", innerException, "CERT_NO_CARGADO")
        {
            RutaCertificado = rutaCertificado;
            AgregarDato("rutaCertificado", rutaCertificado);
        }
    }

    // ============================================================
    // T-066: EXCEPCIONES DE XML
    // ============================================================

    /// <summary>
    /// Excepción cuando el XML no es válido
    /// </summary>
    public class XmlInvalidoException : FirmaElectronicaException
    {
        public int? LineaError { get; set; }
        public int? PosicionError { get; set; }

        public XmlInvalidoException(string mensaje)
            : base($"XML inválido: {mensaje}", "XML_INVALIDO")
        {
        }

        public XmlInvalidoException(string mensaje, Exception innerException)
            : base($"XML inválido: {mensaje}", innerException, "XML_INVALIDO")
        {
            // Intentar extraer información del error XML
            if (innerException is System.Xml.XmlException xmlEx)
            {
                LineaError = xmlEx.LineNumber;
                PosicionError = xmlEx.LinePosition;
                AgregarDato("lineaError", xmlEx.LineNumber);
                AgregarDato("posicionError", xmlEx.LinePosition);
            }
        }
    }

    /// <summary>
    /// T-066: Excepción cuando el XML ya está firmado
    /// </summary>
    public class XmlYaFirmadoException : FirmaElectronicaException
    {
        public XmlYaFirmadoException()
            : base("El XML ya tiene firma digital. No se puede firmar nuevamente.", "XML_YA_FIRMADO")
        {
        }

        public XmlYaFirmadoException(string detalles)
            : base($"El XML ya tiene firma digital: {detalles}", "XML_YA_FIRMADO")
        {
        }
    }

    /// <summary>
    /// T-066: Excepción cuando falta el nodo a firmar
    /// </summary>
    public class NodoNoEncontradoException : FirmaElectronicaException
    {
        public string IdNodo { get; }

        public NodoNoEncontradoException(string idNodo)
            : base($"No se encontró el nodo con id='{idNodo}' en el XML", "NODO_NO_ENCONTRADO")
        {
            IdNodo = idNodo;
            AgregarDato("idNodo", idNodo);
        }
    }

    // ============================================================
    // T-066: EXCEPCIONES DE FIRMA
    // ============================================================

    /// <summary>
    /// Excepción cuando la firma falla
    /// </summary>
    public class ErrorFirmaException : FirmaElectronicaException
    {
        public string? Etapa { get; set; }

        public ErrorFirmaException(string mensaje, string? etapa = null)
            : base($"Error al firmar documento: {mensaje}", "FIRMA_FALLO")
        {
            Etapa = etapa;
            if (!string.IsNullOrEmpty(etapa))
            {
                AgregarDato("etapa", etapa);
            }
        }

        public ErrorFirmaException(string mensaje, Exception innerException, string? etapa = null)
            : base($"Error al firmar documento: {mensaje}", innerException, "FIRMA_FALLO")
        {
            Etapa = etapa;
            if (!string.IsNullOrEmpty(etapa))
            {
                AgregarDato("etapa", etapa);
            }
        }
    }

    /// <summary>
    /// T-066: Excepción cuando falla el cálculo de digest
    /// </summary>
    public class ErrorCalculoDigestException : FirmaElectronicaException
    {
        public string TipoDigest { get; }

        public ErrorCalculoDigestException(string tipoDigest, string mensaje)
            : base($"Error al calcular digest de {tipoDigest}: {mensaje}", "DIGEST_ERROR")
        {
            TipoDigest = tipoDigest;
            AgregarDato("tipoDigest", tipoDigest);
        }

        public ErrorCalculoDigestException(string tipoDigest, Exception innerException)
            : base($"Error al calcular digest de {tipoDigest}", innerException, "DIGEST_ERROR")
        {
            TipoDigest = tipoDigest;
            AgregarDato("tipoDigest", tipoDigest);
        }
    }

    /// <summary>
    /// T-066: Excepción cuando falla la firma RSA
    /// </summary>
    public class ErrorFirmaRsaException : FirmaElectronicaException
    {
        public int? KeySize { get; set; }

        public ErrorFirmaRsaException(string mensaje)
            : base($"Error en firma RSA: {mensaje}", "RSA_ERROR")
        {
        }

        public ErrorFirmaRsaException(string mensaje, Exception innerException, int? keySize = null)
            : base($"Error en firma RSA: {mensaje}", innerException, "RSA_ERROR")
        {
            KeySize = keySize;
            if (keySize.HasValue)
            {
                AgregarDato("keySize", keySize.Value);
            }
        }
    }

    // ============================================================
    // T-066: EXCEPCIONES DE VALIDACIÓN
    // ============================================================

    /// <summary>
    /// Excepción cuando la validación de firma falla
    /// </summary>
    public class ValidacionFirmaException : FirmaElectronicaException
    {
        public List<string> Errores { get; set; }

        public ValidacionFirmaException(string mensaje, List<string>? errores = null)
            : base($"Error al validar firma: {mensaje}", "VALIDACION_ERROR")
        {
            Errores = errores ?? new List<string>();
            if (Errores.Any())
            {
                AgregarDato("errores", string.Join("; ", Errores));
            }
        }

        public ValidacionFirmaException(string mensaje, Exception innerException)
            : base($"Error al validar firma: {mensaje}", innerException, "VALIDACION_ERROR")
        {
            Errores = new List<string>();
        }
    }

    /// <summary>
    /// T-066: Excepción cuando la firma no es válida
    /// </summary>
    public class FirmaInvalidaException : FirmaElectronicaException
    {
        public List<string> RazonesInvalidez { get; set; }

        public FirmaInvalidaException(string razon)
            : base($"La firma digital no es válida: {razon}", "FIRMA_INVALIDA")
        {
            RazonesInvalidez = new List<string> { razon };
        }

        public FirmaInvalidaException(List<string> razones)
            : base($"La firma digital no es válida. Errores encontrados: {razones.Count}", "FIRMA_INVALIDA")
        {
            RazonesInvalidez = razones;
            AgregarDato("errores", string.Join("; ", razones));
        }
    }

    /// <summary>
    /// T-066: Excepción cuando falta la firma
    /// </summary>
    public class FirmaNoEncontradaException : FirmaElectronicaException
    {
        public FirmaNoEncontradaException()
            : base("El XML no contiene firma digital", "FIRMA_NO_ENCONTRADA")
        {
        }

        public FirmaNoEncontradaException(string detalles)
            : base($"El XML no contiene firma digital: {detalles}", "FIRMA_NO_ENCONTRADA")
        {
        }
    }

    // ============================================================
    // T-066: EXCEPCIONES DE CONFIGURACIÓN
    // ============================================================

    /// <summary>
    /// T-066: Excepción cuando la configuración es inválida
    /// </summary>
    public class ConfiguracionInvalidaException : FirmaElectronicaException
    {
        public string ParametroInvalido { get; }

        public ConfiguracionInvalidaException(string parametro, string mensaje)
            : base($"Configuración inválida en '{parametro}': {mensaje}", "CONFIG_INVALIDA")
        {
            ParametroInvalido = parametro;
            AgregarDato("parametro", parametro);
        }
    }

    /// <summary>
    /// T-066: Excepción cuando falta configuración requerida
    /// </summary>
    public class ConfiguracionFaltanteException : FirmaElectronicaException
    {
        public List<string> ParametrosFaltantes { get; set; }

        public ConfiguracionFaltanteException(string parametro)
            : base($"Falta configuración requerida: {parametro}", "CONFIG_FALTANTE")
        {
            ParametrosFaltantes = new List<string> { parametro };
            AgregarDato("parametrosFaltantes", parametro);
        }

        public ConfiguracionFaltanteException(List<string> parametros)
            : base($"Faltan {parametros.Count} parámetros de configuración requeridos", "CONFIG_FALTANTE")
        {
            ParametrosFaltantes = parametros;
            AgregarDato("parametrosFaltantes", string.Join(", ", parametros));
        }
    }
}