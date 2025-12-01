namespace SistemaFacturacionSRI.Domain.DTOs.Factura
{
    /// <summary>
    /// Resultado de la validación de un XML contra el esquema XSD del SRI
    /// </summary>
    public class ResultadoValidacion
    {
        public bool EsValido { get; set; }
        public List<ErrorValidacion> Errores { get; set; } = new List<ErrorValidacion>();
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static ResultadoValidacion Exitoso()
        {
            return new ResultadoValidacion
            {
                EsValido = true,
                Mensaje = "XML válido según esquema XSD del SRI"
            };
        }

        /// <summary>
        /// Crea un resultado con errores
        /// </summary>
        public static ResultadoValidacion ConErrores(List<ErrorValidacion> errores)
        {
            return new ResultadoValidacion
            {
                EsValido = false,
                Errores = errores,
                Mensaje = $"Se encontraron {errores.Count} error(es) de validación"
            };
        }
    }

    /// <summary>
    /// Representa un error de validación XML
    /// </summary>
    public class ErrorValidacion
    {
        public int Linea { get; set; }
        public int Posicion { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty; // "Error" o "Advertencia"

        public override string ToString()
        {
            return $"[{Severidad}] Línea {Linea}, Pos {Posicion}: {Mensaje}";
        }
    }
}