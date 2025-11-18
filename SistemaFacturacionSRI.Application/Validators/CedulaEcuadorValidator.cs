using System.Linq;

namespace SistemaFacturacionSRI.Application.Validators
{
    /// <summary>
    /// Validador básico para cédulas ecuatorianas (10 dígitos numéricos).
    /// </summary>
    public static class CedulaEcuadorValidator
    {
        private const int CedulaLength = 10;
        private static readonly int[] Coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };

        /// <summary>
        /// Valida la cédula ecuatoriana y retorna solamente un indicador booleano.
        /// </summary>
        public static bool EsValida(string? cedula) => TryValidar(cedula, out _);

        /// <summary>
        /// Intenta validar la cédula ecuatoriana y retorna un mensaje descriptivo cuando no es válida.
        /// </summary>
        public static bool TryValidar(string? cedula, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(cedula))
            {
                errorMessage = "La cédula no puede estar vacía.";
                return false;
            }

            var normalizada = cedula.Trim();

            if (normalizada.Length != CedulaLength || !normalizada.All(char.IsDigit))
            {
                errorMessage = "La cédula debe contener exactamente 10 dígitos numéricos.";
                return false;
            }

            if (!int.TryParse(normalizada[..2], out var provincia) || provincia < 1 || provincia > 24)
            {
                errorMessage = "Los dos primeros dígitos deben pertenecer a una provincia válida (01-24).";
                return false;
            }

            if (!ValidarDigitoVerificador(normalizada))
            {
                errorMessage = "El dígito verificador es inválido.";
                return false;
            }

            return true;
        }

        private static bool ValidarDigitoVerificador(string cedula)
        {
            var suma = 0;

            for (var i = 0; i < CedulaLength - 1; i++)
            {
                var valor = int.Parse(cedula[i].ToString()) * Coeficientes[i];
                suma += valor > 9 ? valor - 9 : valor;
            }

            var residuo = suma % 10;
            var digitoVerificadorCalculado = residuo == 0 ? 0 : 10 - residuo;
            var digitoVerificadorCedula = int.Parse(cedula[^1].ToString());

            return digitoVerificadorCalculado == digitoVerificadorCedula;
        }
    }
}
