namespace SistemaFacturacionSRI.Domain.Enums
{
    /// <summary>
    /// Métodos de extensión para el enum TipoCliente.
    /// Facilita la obtención de información y validaciones del tipo de cliente.
    /// </summary>
    public static class TipoClienteExtensions
    {
        /// <summary>
        /// Obtiene el nombre descriptivo del tipo de cliente
        /// </summary>
        public static string ObtenerNombre(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => "Persona Natural",
                TipoCliente.Empresa => "Empresa",
                TipoCliente.Pasaporte => "Extranjero (Pasaporte)",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Obtiene la descripción detallada del tipo de cliente
        /// </summary>
        public static string ObtenerDescripcion(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => "Cliente ecuatoriano que usa cédula de identidad (10 dígitos)",
                TipoCliente.Empresa => "Empresa o persona jurídica que usa RUC (13 dígitos)",
                TipoCliente.Pasaporte => "Cliente extranjero identificado con número de pasaporte",
                _ => "Tipo de cliente no definido"
            };
        }

        /// <summary>
        /// Verifica si el tipo de cliente es persona natural
        /// </summary>
        public static bool EsPersonaNatural(this TipoCliente tipo)
        {
            return tipo == TipoCliente.PersonaNatural;
        }

        /// <summary>
        /// Verifica si el tipo de cliente es empresa
        /// </summary>
        public static bool EsEmpresa(this TipoCliente tipo)
        {
            return tipo == TipoCliente.Empresa;
        }

        /// <summary>
        /// Verifica si el tipo de cliente es extranjero con pasaporte
        /// </summary>
        public static bool EsExtranjero(this TipoCliente tipo)
        {
            return tipo == TipoCliente.Pasaporte;
        }

        /// <summary>
        /// Obtiene el tipo de documento de identificación requerido
        /// </summary>
        public static string ObtenerTipoDocumento(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => "Cédula",
                TipoCliente.Empresa => "RUC",
                TipoCliente.Pasaporte => "Pasaporte",
                _ => "Documento no especificado"
            };
        }

        /// <summary>
        /// Obtiene la longitud esperada del documento de identificación
        /// Retorna 0 si la longitud es variable (como en pasaportes)
        /// </summary>
        public static int ObtenerLongitudDocumento(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => 10, // Cédula: 10 dígitos
                TipoCliente.Empresa => 13,        // RUC: 13 dígitos
                TipoCliente.Pasaporte => 0,       // Variable: 6-20 caracteres alfanuméricos
                _ => 0
            };
        }

        /// <summary>
        /// Obtiene el rango de longitud del documento (mínimo, máximo)
        /// </summary>
        public static (int Min, int Max) ObtenerRangoLongitudDocumento(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => (10, 10),  // Exactamente 10
                TipoCliente.Empresa => (13, 13),         // Exactamente 13
                TipoCliente.Pasaporte => (6, 20),        // Entre 6 y 20
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Valida que la longitud del documento coincida con el tipo de cliente
        /// </summary>
        public static bool ValidarLongitudDocumento(this TipoCliente tipo, string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return false;

            var (min, max) = tipo.ObtenerRangoLongitudDocumento();
            var longitud = documento.Trim().Length;

            return longitud >= min && longitud <= max;
        }

        /// <summary>
        /// Valida que el formato del documento sea correcto según el tipo
        /// </summary>
        public static bool ValidarFormatoDocumento(this TipoCliente tipo, string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return false;

            documento = documento.Trim();

            return tipo switch
            {
                // Cédula: exactamente 10 dígitos numéricos
                TipoCliente.PersonaNatural => documento.Length == 10 && 
                                              documento.All(char.IsDigit),
                
                // RUC: exactamente 13 dígitos numéricos
                TipoCliente.Empresa => documento.Length == 13 && 
                                       documento.All(char.IsDigit),
                
                // Pasaporte: 6-20 caracteres alfanuméricos (letras y números)
                TipoCliente.Pasaporte => documento.Length >= 6 && 
                                         documento.Length <= 20 && 
                                         documento.All(c => char.IsLetterOrDigit(c)),
                
                _ => false
            };
        }

        /// <summary>
        /// Obtiene un mensaje de error descriptivo si el formato es inválido
        /// </summary>
        public static string ObtenerMensajeErrorFormato(this TipoCliente tipo)
        {
            return tipo switch
            {
                TipoCliente.PersonaNatural => "La cédula debe contener exactamente 10 dígitos numéricos",
                TipoCliente.Empresa => "El RUC debe contener exactamente 13 dígitos numéricos",
                TipoCliente.Pasaporte => "El pasaporte debe contener entre 6 y 20 caracteres alfanuméricos",
                _ => "Formato de documento inválido"
            };
        }

        /// <summary>
        /// Verifica si el documento requiere validación de dígito verificador
        /// (Cédula y RUC ecuatorianos tienen algoritmo de validación)
        /// </summary>
        public static bool RequiereValidacionDigitoVerificador(this TipoCliente tipo)
        {
            return tipo == TipoCliente.PersonaNatural || tipo == TipoCliente.Empresa;
        }

        /// <summary>
        /// Convierte un string a enum TipoCliente de forma segura
        /// </summary>
        public static TipoCliente? ConvertirDesdeString(string tipoString)
        {
            if (string.IsNullOrWhiteSpace(tipoString))
                return null;

            return tipoString.Trim().ToLower() switch
            {
                "persona natural" or "natural" or "persona" or "cedula" => TipoCliente.PersonaNatural,
                "empresa" or "juridica" or "persona juridica" or "ruc" => TipoCliente.Empresa,
                "pasaporte" or "extranjero" or "extranjera" or "passport" => TipoCliente.Pasaporte,
                _ => null
            };
        }

        /// <summary>
        /// Obtiene todos los tipos de cliente disponibles
        /// </summary>
        public static List<(TipoCliente Tipo, string Nombre, string Documento, string Formato)> ObtenerTodosLosTipos()
        {
            return new List<(TipoCliente, string, string, string)>
            {
                (TipoCliente.PersonaNatural, "Persona Natural", "Cédula", "10 dígitos numéricos"),
                (TipoCliente.Empresa, "Empresa", "RUC", "13 dígitos numéricos"),
                (TipoCliente.Pasaporte, "Extranjero", "Pasaporte", "6-20 caracteres alfanuméricos")
            };
        }

        /// <summary>
        /// Normaliza el documento (elimina espacios, guiones, puntos)
        /// </summary>
        public static string NormalizarDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return string.Empty;

            // Eliminar espacios, guiones, puntos
            return documento.Trim()
                           .Replace(" ", "")
                           .Replace("-", "")
                           .Replace(".", "")
                           .ToUpper(); // Mayúsculas para pasaportes
        }
    }
}